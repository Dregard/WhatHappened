using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Anomalies;
using Game.Boosters;
using Game.Levels;
using Game.Profile;
using Game.Purchase;
using Game.UI.Hud;
using Game.UI.Windows.General;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Zenject;

namespace Game.Gameplay.Chapters
{
    public sealed class ChapterSystem : IInitializable, IDisposable
    {
        private readonly GameplayProgressController _progress;
        private readonly ProfileSystem _profiles;
        private readonly RewardSystem _rewards;
        private readonly WindowSystem _windows;
        private readonly HudView _hud;
        private readonly List<AsyncOperationHandle<LevelData>> _clueHandlesList = new();
        private ChapterHudView _view;
        private GameObject _templates;
        private LevelsData _data;
        private Sprite[] _cluesArray;
        private int _generation;
        private bool _isOpening;
        private bool _isPresenting;
        private bool _isCompletingLevel;

        /// <summary>Connects progression, existing rewards/profile persistence and passive UI at the composition root.</summary>
        public ChapterSystem(GameplayProgressController progress, ProfileSystem profiles, RewardSystem rewards, WindowSystem windows, HudView hud)
        {
            _progress = progress;
            _profiles = profiles;
            _rewards = rewards;
            _windows = windows;
            _hud = hud;
        }

        /// <summary>Registers a hierarchy-owned template in the existing window system without changing scenes or authored prefabs.</summary>
        public void Initialize()
        {
            _templates = new GameObject("ChapterTemplates");
            _templates.transform.SetParent(_windows.transform, false);
            _templates.SetActive(false);
            var template = new GameObject("ChapterWindow", typeof(RectTransform));
            template.layer = _windows.gameObject.layer;
            template.transform.SetParent(_templates.transform, false);
            var window = template.AddComponent<ChapterWindow>();
            template.AddComponent<ChapterWindowController>();
            template.AddComponent<GraphicRaycaster>();
            _windows.RegisterTemplate(window);
        }

        /// <summary>Creates a fresh level-owned counter and releases previous clue asset consumers before their handles.</summary>
        public void Prepare(LevelsData data)
        {
            Cancel();
            _data = data;
            if (!_progress.UsesChapters)
                return;
            var root = new GameObject("ChapterHud", typeof(RectTransform));
            root.layer = _hud.HudCanvas.gameObject.layer;
            root.transform.SetParent(_hud.HudCanvas.transform, false);
            AnomalyUi.Bounds((RectTransform)root.transform, Vector2.zero, Vector2.one);
            _view = root.AddComponent<ChapterHudView>();
            _view.Initialize(_progress.CurrentChapter.Title, () => ShowInvestigation().Forget());
            Refresh();
        }

        /// <summary>Cancels an obsolete result sequence and asset loads by generation; loaded sprites are released only after their UI consumers are removed.</summary>
        public void Cancel()
        {
            _generation++;
            _isOpening = false;
            _isPresenting = false;
            _isCompletingLevel = false;
            if (_view != null)
            {
                _view.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_view.gameObject);
                _view = null;
            }
            _cluesArray = null;
            foreach (var handle in _clueHandlesList)
                Addressables.Release(handle);
            _clueHandlesList.Clear();
        }

        /// <summary>Blocks investigation immediately when search finishes, including an investigation load whose window has not opened yet.</summary>
        public void BeginCompletion()
        {
            _isCompletingLevel = true;
            if (_view != null)
                _view.Show(_progress.ClueCount, _progress.CurrentChapter.LevelsArray.Length, false);
        }

        /// <summary>Shows the evidence-found screen for a just completed level. The clue is committed once, when it lands in its slot; later screens follow the level or chapter outcome.</summary>
        public async UniTask ShowResult(Sprite clue, string clueName, Action continueLevel, Action continueChapter)
        {
            if (_progress.IsLevelVictoryPending || _progress.IsChapterComplete || _isPresenting)
                return;
            _isPresenting = true;
            var generation = _generation;
            await LoadClues();
            if (generation != _generation)
                throw new OperationCanceledException();
            var total = _progress.CurrentChapter.LevelsArray.Length;
            var model = MakeModel(ChapterWindowModeAlias.EvidenceFound, continueLevel, continueChapter);
            model.NewClue = clue;
            model.NewClueName = clueName;
            model.NewClueIndex = _progress.ClueCount;
            model.IsFinal = _progress.ClueCount + 1 == total;
            model.CommitClue = CommitClue;
            await _windows.Open<ChapterWindow>(new ChapterWindowModelParams { Model = model });
            _isPresenting = false;
        }

        /// <summary>Resumes the screen matching saved progress: the level reward when only that was pending, or the culprit sequence when the chapter is complete.</summary>
        public async UniTask Resume(Action continueLevel, Action continueChapter)
        {
            await LoadClues();
            var isChapter = _progress.IsChapterComplete;
            var model = MakeModel(isChapter ? ChapterWindowModeAlias.AllEvidence : ChapterWindowModeAlias.LevelReward, continueLevel, continueChapter);
            model.IsFinal = isChapter;
            await _windows.Open<ChapterWindow>(new ChapterWindowModelParams { Model = model });
        }

        /// <summary>Commits the clue to progression at slot arrival and persists the completed level.</summary>
        private UniTask CommitClue()
        {
            _progress.CollectClue();
            _profiles.Save();
            Refresh();
            return UniTask.CompletedTask;
        }

        /// <summary>Grants the non-final level reward once, then acknowledges the level and persists balance and progress together.</summary>
        private async UniTask ClaimLevelReward()
        {
            if (!_progress.IsLevelVictoryPending)
                return;
            var rewards = _progress.CurrentChapter.LevelRewardsList;
            if (rewards != null && rewards.Count > 0)
                await _rewards.GiveRewards(rewards);
            _progress.AcknowledgeLevelVictory();
            _profiles.Save();
        }

        /// <summary>Acknowledges the final level, then grants the guarded chapter reward and saves the claim with balances.</summary>
        private async UniTask ClaimChapterReward()
        {
            if (_progress.IsLevelVictoryPending)
            {
                _progress.AcknowledgeLevelVictory();
                _profiles.Save();
            }
            if (_progress.TryClaimChapterReward())
            {
                await _rewards.GiveRewards(_progress.CurrentChapter.RewardsList);
                _profiles.Save();
            }
        }

        /// <summary>Displays the current chapter's found evidence and the unsolved culprit; no historical chapter selection exists.</summary>
        private async UniTask ShowInvestigation()
        {
            if (_isOpening || _isPresenting || _isCompletingLevel || _progress.IsLevelVictoryPending || _progress.IsChapterComplete || _windows.OpenWindowsCount > 0)
                return;
            _isOpening = true;
            var generation = _generation;
            try
            {
                await LoadClues();
                if (generation != _generation || _isCompletingLevel)
                    return;
                var model = MakeModel(ChapterWindowModeAlias.Investigation, null, null);
                model.CloseAction = () => _windows.Close<ChapterWindow>().Forget();
                await _windows.Open<ChapterWindow>(new ChapterWindowModelParams { Model = model });
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (generation == _generation)
                    _isOpening = false;
            }
        }

        /// <summary>Builds a snapshot with exactly one slot per configured level, exposing found sprites only.</summary>
        private ChapterWindowModel MakeModel(ChapterWindowModeAlias mode, Action continueLevel, Action continueChapter)
        {
            var chapter = _progress.CurrentChapter;
            var model = new ChapterWindowModel
            {
                Mode = mode,
                ChapterTitle = chapter.Title,
                CluesArray = _cluesArray.Select((sprite, index) => index < _progress.ClueCount ? sprite : null).ToArray(),
                CulpritPortrait = chapter.CulpritPortrait,
                CulpritCaption = chapter.CulpritCaption,
                CulpritSceneImage = chapter.CulpritSceneImage,
                CulpritSceneCaption = chapter.CulpritSceneCaption,
                ClaimLevelReward = ClaimLevelReward,
                ClaimChapterReward = ClaimChapterReward,
                PresentationData = _data,
            };
            model.FinishAction = () => FinishWindow(mode == ChapterWindowModeAlias.AllEvidence || _progress.IsChapterComplete ? continueChapter : continueLevel);
            if (mode != ChapterWindowModeAlias.Investigation)
            {
                model.LevelRewardsList = Describe(chapter.LevelRewardsList, 1);
                model.ChapterRewardsList = Describe(chapter.RewardsList, 3);
            }
            return model;
        }

        /// <summary>Hands control back to the owner once the screens have claimed and saved everything.</summary>
        private void FinishWindow(Action next)
        {
            next?.Invoke();
        }

        /// <summary>Acquires separate Addressables handles for found clue images, retaining them through the current UI lifetime. Stale loads release their own handles.</summary>
        private async UniTask LoadClues()
        {
            var generation = _generation;
            var chapter = _progress.CurrentChapter;
            _cluesArray ??= new Sprite[chapter.LevelsArray.Length];
            for (var index = 0; index < _progress.ClueCount; index++)
            {
                if (_cluesArray[index] != null)
                    continue;
                var handle = Addressables.LoadAssetAsync<LevelData>(chapter.LevelsArray[index].LevelId);
                try
                {
                    var level = await handle.ToUniTask();
                    if (generation != _generation)
                        throw new OperationCanceledException();
                    if (level.SearchData?.ClueImage == null)
                        throw new InvalidOperationException("Collected chapter level has no clue image.");
                    _cluesArray[index] = level.SearchData.ClueImage;
                    _clueHandlesList.Add(handle);
                }
                catch
                {
                    Addressables.Release(handle);
                    throw;
                }
            }
        }

        /// <summary>Formats supported reward kinds for display with the matching themed icons; nothing is granted here.</summary>
        private static List<ChapterRewardModel> Describe(List<RewardData> rewards, int coinCopies)
        {
            var theme = UiThemeData.LoadOrDefault();
            var list = new List<ChapterRewardModel>();
            if (rewards == null)
                return list;
            foreach (var reward in rewards.Where(item => item != null))
            {
                if (reward is CurrencyRewardData currency)
                    list.Add(new ChapterRewardModel { Icon = theme.CoinIcon, Text = $"+{currency.Amount}", IconCount = coinCopies });
                else if (reward is BoosterRewardData booster)
                    list.Add(new ChapterRewardModel
                    {
                        Icon = booster.BoosterKind == BoosterKind.FreezeTimer ? theme.CameraPackIcon : theme.HintPackIcon,
                        Text = $"+{booster.Amount}",
                    });
            }
            return list;
        }

        /// <summary>Reveals the authoritative completed count after arrival or profile restoration.</summary>
        private void Refresh()
        {
            if (_view != null)
                _view.Show(_progress.ClueCount, _progress.CurrentChapter.LevelsArray.Length, !_isCompletingLevel);
        }

        /// <summary>Releases hierarchy-owned presentation and retained clue assets when the gameplay scope ends.</summary>
        public void Dispose()
        {
            Cancel();
            if (_templates != null)
                UnityEngine.Object.Destroy(_templates);
        }
    }
}
