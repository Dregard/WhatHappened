using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.AssetManagement;
using Game.Levels;
using Game.UI.Windows.DownloadWaitWindow;
using Game.UI.Windows.General;
using UnityEngine;

namespace Game.Gameplay
{
    public class LevelContentSystem : IDisposable
    {
        private readonly AddressablesAssetSystem _assetSystem;
        private readonly GameplayProgressController _progressController;
        private readonly WindowSystem _windowSystem;
        private readonly CancellationTokenSource _lifetimeCancellation = new();
        private readonly HashSet<int> _pendingEvictionsSet = new();
        private LevelsData _levelsData;
        private bool _isRunning;
        private bool _isDisposed;
        private int _generation;
        private UniTaskCompletionSource _reserveChangedSource = new();

        /// <summary>Owns the background disk reserve and retry interaction for the gameplay scope; never loads level assets into memory.</summary>
        public LevelContentSystem(AddressablesAssetSystem assetSystem, GameplayProgressController progressController, WindowSystem windowSystem)
        {
            _assetSystem = assetSystem;
            _progressController = progressController;
            _windowSystem = windowSystem;
        }

        /// <summary>Applies authored settings and schedules cleanup of completed non-loop content from earlier sessions.</summary>
        public void Configure(LevelsData levelsData)
        {
            if (_levelsData != null)
            {
                return;
            }
            _levelsData = levelsData;
            _progressController.Configure(levelsData);
            if (_progressController.UsesChapters)
                return;
            var loopStartIndex = levelsData.Levels.Length - levelsData.LoopLevelCount;
            var completedCount = Math.Min(_progressController.CurrentLevel - 1, loopStartIndex);
            for (var index = 0; index < completedCount; index++)
            {
                _pendingEvictionsSet.Add(index);
            }
        }

        /// <summary>Waits only for the required level. Cached content bypasses background downloads; failures wait for explicit Retry. Shutdown cancels the waiter, while acquired Addressables operations finish and release their handles.</summary>
        public async UniTask EnsureCurrentLevelAsync(int levelIndex, IProgress<float> progress = null)
        {
            var hasWaitingWindow = false;
            try
            {
                while (!_isDisposed)
                {
                    try
                    {
                        var key = _levelsData.Levels[levelIndex].LevelId;
                        if (await _assetSystem.GetDownloadSizeAsync(key) > 0)
                        {
                            await _assetSystem.DownloadAsync(key, progress);
                        }
                        _lifetimeCancellation.Token.ThrowIfCancellationRequested();
                        progress?.Report(1f);
                        return;
                    }
                    catch (Exception exception) when (!(exception is OperationCanceledException))
                    {
                        _lifetimeCancellation.Token.ThrowIfCancellationRequested();
                        Debug.LogWarning($"Level download unavailable: {exception.Message}");
                        var retrySource = new UniTaskCompletionSource();
                        await _windowSystem.Open<DownloadWaitWindow>(new DownloadWaitWindowModelParams
                        {
                            RetryAction = () => retrySource.TrySetResult(),
                        });
                        hasWaitingWindow = true;
                        await retrySource.Task.AttachExternalCancellation(_lifetimeCancellation.Token);
                        await _windowSystem.Close<DownloadWaitWindow>();
                        hasWaitingWindow = false;
                    }
                }
                throw new OperationCanceledException(_lifetimeCancellation.Token);
            }
            finally
            {
                if (hasWaitingWindow && _windowSystem != null && !_isDisposed)
                {
                    await _windowSystem.Close<DownloadWaitWindow>();
                }
            }
        }

        /// <summary>Restarts reserve planning after readiness or a transition. A single owner retries missing content at the authored interval.</summary>
        public void MaintainReserve()
        {
            _generation++;
            _reserveChangedSource.TrySetResult();
            if (!_isRunning && !_isDisposed)
            {
                RunAsync().Forget();
            }
        }

        /// <summary>Marks an unloaded level for disk eviction only when it is completed and outside the retained final set.</summary>
        public void HandleLevelReleased(int levelIndex)
        {
            if (_progressController.UsesChapters)
                return;
            var loopStartIndex = _levelsData.Levels.Length - _levelsData.LoopLevelCount;
            if (levelIndex < loopStartIndex && levelIndex < _progressController.CurrentLevel - 1)
            {
                _pendingEvictionsSet.Add(levelIndex);
            }
            _generation++;
            _reserveChangedSource.TrySetResult();
        }

        /// <summary>Downloads the bounded unique reserve then retries evictions. Generation changes stop outdated planning; disposal prevents further operations.</summary>
        private async UniTask RunAsync()
        {
            _isRunning = true;
            try
            {
                while (!_isDisposed)
                {
                    var generation = _generation;
                    _reserveChangedSource = new UniTaskCompletionSource();
                    await EvictCompletedAsync();
                    var targetsList = GetReserveIndices();
                    foreach (var index in targetsList)
                    {
                        if (_isDisposed || generation != _generation)
                        {
                            break;
                        }
                        try
                        {
                            var key = _levelsData.Levels[index].LevelId;
                            if (await _assetSystem.GetDownloadSizeAsync(key) > 0 && !_isDisposed && generation == _generation)
                            {
                                await _assetSystem.DownloadAsync(key);
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning($"Background level download postponed: {exception.Message}");
                        }
                    }
                    if (_isDisposed)
                    {
                        break;
                    }
                    if (generation == _generation)
                    {
                        using (var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token))
                        {
                            await UniTask.WhenAny(
                                UniTask.Delay(TimeSpan.FromSeconds(_levelsData.DownloadRetryInterval), ignoreTimeScale: true, cancellationToken: delayCancellation.Token),
                                _reserveChangedSource.Task.AttachExternalCancellation(delayCancellation.Token));
                            delayCancellation.Cancel();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown cancels the delay only; in-flight content operations are awaited before returning.
            }
            finally
            {
                _isRunning = false;
            }
        }

        /// <summary>Plans current and following distinct levels, clamping the reserve to the reachable main sequence and final loop.</summary>
        private List<int> GetReserveIndices()
        {
            var indicesList = new List<int>();
            var indicesSet = new HashSet<int>();
            var targetCount = Math.Min(_levelsData.DownloadReserveCount, _levelsData.Levels.Length);
            for (var offset = 0; offset < _levelsData.Levels.Length + _levelsData.LoopLevelCount && indicesList.Count < targetCount; offset++)
            {
                var index = _progressController.GetLevelIndexAtOffset(offset);
                if (indicesSet.Add(index))
                {
                    indicesList.Add(index);
                }
            }
            return indicesList;
        }

        /// <summary>Preserves all uncompleted and final-loop dependencies, including earlier-session downloads and loaded local content.</summary>
        private async UniTask EvictCompletedAsync()
        {
            if (_progressController.UsesChapters)
                return;
            var retainedKeysList = new List<string>();
            var loopStartIndex = _levelsData.Levels.Length - _levelsData.LoopLevelCount;
            var retainStartIndex = Math.Min(_progressController.CurrentLevel - 1, loopStartIndex);
            for (var index = retainStartIndex; index < _levelsData.Levels.Length; index++)
            {
                retainedKeysList.Add(_levelsData.Levels[index].LevelId);
            }
            foreach (var index in new List<int>(_pendingEvictionsSet))
            {
                if (_isDisposed)
                {
                    return;
                }
                try
                {
                    if (await _assetSystem.RemoveDownloadedLevelAsync(_levelsData.Levels[index].LevelId, retainedKeysList))
                    {
                        _pendingEvictionsSet.Remove(index);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Level cache cleanup postponed: {exception.Message}");
                }
            }
        }

        /// <summary>Stops planning and retry waits. Existing Addressables operations still own and release their handles when they finish.</summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _lifetimeCancellation.Cancel();
        }
    }
}
