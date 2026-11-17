using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace Game.AssetManagement
{
    public class AddressablesAssetSystem : IAssetSystem
    {
        private readonly Dictionary<string, AssetModel> _assetById = new();
        private readonly Dictionary<string, Sprite> _sprites = new();
        private readonly AddressablesCacheController _cacheController = new();
        private readonly SemaphoreSlim _contentOperations = new(1, 1);
        private readonly SemaphoreSlim _downloadOperations = new(1, 1);
        private readonly Dictionary<SpriteAtlas, List<Sprite>> _spritesByAtlas = new();

        private bool _isLoadingInProgress;
        

        /// <summary>Creates the scoped asset owner and connects the existing sprite presentation callback.</summary>
        public AddressablesAssetSystem()
        {
            AssetHelper.GetSprite = GetSprite;
        }
        
        /// <summary>Serializes content loads and retains resources until Release. Optional progress averages the five resource groups and their Addressables operations; failed loads release acquired resources.</summary>
        public async UniTask LoadAsync(string bundleName, IProgress<float> progress = null)
        {
            await _contentOperations.WaitAsync();
            _isLoadingInProgress = true;
            try
            {
                var groupProgressArray = new float[5];
                // Task.WhenAll waits for all siblings even when one fails, so rollback cannot race a late load.
                await System.Threading.Tasks.Task.WhenAll(
                    LoadAssets<GameObject>(bundleName, CreateGroupProgress(progress, groupProgressArray, 0)).AsTask(),
                    LoadAssets<ScriptableObject>(bundleName, CreateGroupProgress(progress, groupProgressArray, 1)).AsTask(),
                    LoadAssets<SpriteAtlas>(bundleName, CreateGroupProgress(progress, groupProgressArray, 2)).AsTask(),
                    LoadAssets<Material>(bundleName, CreateGroupProgress(progress, groupProgressArray, 3)).AsTask(),
                    LoadScenes(bundleName, CreateGroupProgress(progress, groupProgressArray, 4)).AsTask());
                CollectSprites();
                progress?.Report(1f);
            }
            catch
            {
                _isLoadingInProgress = false;
                Release(bundleName);
                throw;
            }
            finally
            {
                _isLoadingInProgress = false;
                _contentOperations.Release();
            }
        }

        /// <summary>Aggregates known resource groups without treating discovery of another group as a progress reset.</summary>
        private IProgress<float> CreateGroupProgress(IProgress<float> progress, float[] groupProgressArray, int index)
        {
            return Progress.Create<float>(value =>
            {
                groupProgressArray[index] = value;
                progress?.Report(groupProgressArray.Sum() / groupProgressArray.Length);
            });
        }

        /// <summary>Returns bytes still missing from disk without loading resources. The query handle is released by the cache controller.</summary>
        public UniTask<long> GetDownloadSizeAsync(string contentKey)
        {
            return _cacheController.GetDownloadSizeAsync(contentKey);
        }

        /// <summary>Serializes disk downloads without blocking loads of already cached levels. Owns each operation until completion.</summary>
        public async UniTask DownloadAsync(string contentKey, IProgress<float> progress = null)
        {
            await _downloadOperations.WaitAsync();
            try
            {
                await _cacheController.DownloadAsync(contentKey, progress);
            }
            finally
            {
                _downloadOperations.Release();
            }
        }

        /// <summary>
        /// Deletes cached versions unused by the supplied retained levels or loaded content. Omitting the retained set protects all other catalog levels.
        /// Queues behind downloads/loads; refuses deletion of a loaded level.
        /// </summary>
        public async UniTask<bool> RemoveDownloadedLevelAsync(string levelId, IEnumerable<string> retainedLevelIdsEnumerable = null)
        {
            await _downloadOperations.WaitAsync();
            await _contentOperations.WaitAsync();
            var locationsHandle = Addressables.LoadResourceLocationsAsync("Level");
            try
            {
                var retainedKeysSet = new HashSet<string>(_assetById.Values.SelectMany(model => model.ContentKeysSet));
                if (retainedKeysSet.Contains(levelId))
                {
                    throw new InvalidOperationException($"Release loaded level '{levelId}' before deleting its downloaded content.");
                }
                var locationsList = await locationsHandle.ToUniTask();
                if (!locationsList.Any(location => location.PrimaryKey == levelId))
                {
                    throw new ArgumentException($"'{levelId}' is not a catalog level.", nameof(levelId));
                }
                foreach (var retainedKey in retainedLevelIdsEnumerable ?? locationsList.Where(location => location.PrimaryKey != levelId).Select(location => location.PrimaryKey))
                {
                    retainedKeysSet.Add(retainedKey);
                }
                return await _cacheController.RemoveAsync(levelId, retainedKeysSet);
            }
            finally
            {
                Addressables.Release(locationsHandle);
                _contentOperations.Release();
                _downloadOperations.Release();
            }
        }
        
        /// <summary>Acquires each matching asset once and records the content keys sharing its ownership.</summary>
        private async UniTask LoadAssets<TAssetType>(string bundleName, IProgress<float> progress)
        {
            var tasks = new List<UniTask>();

            var locationsHandle = Addressables.LoadResourceLocationsAsync(bundleName, typeof(TAssetType));
            IList<IResourceLocation> locations;
            try
            {
                locations = new List<IResourceLocation>(await locationsHandle.ToUniTask());
            }
            finally
            {
                Addressables.Release(locationsHandle);
            }
            var completedProgress = 0f;
            foreach (var location in locations)
            {
                var assetId = location.PrimaryKey;
                if (_assetById.ContainsKey(assetId))
                {
                    _assetById[assetId].ContentKeysSet.Add(bundleName);
                    completedProgress++;
                    continue;
                }

                var loadHandle = Addressables.LoadAssetAsync<TAssetType>(location);
                var previousProgress = 0f;
                tasks.Add(loadHandle.ToUniTask(progress: Progress.Create<float>(value =>
                {
                    completedProgress += value - previousProgress;
                    previousProgress = value;
                    progress?.Report(completedProgress / locations.Count);
                })));

                _assetById[assetId] = new AssetModel()
                {
                    AssetId = assetId,
                    ContentKeysSet = new HashSet<string> { bundleName },
                    LoadHandle = loadHandle,
                };
            }

            await System.Threading.Tasks.Task.WhenAll(tasks.Select(task => task.AsTask()));
            progress?.Report(1f);
        }
        
        /// <summary>Loads matching scenes additively and retains their handles until the final content owner releases them.</summary>
        private async UniTask LoadScenes(string bundleName, IProgress<float> progress)
        {
            var tasks = new List<UniTask>();

            var locationsHandle = Addressables.LoadResourceLocationsAsync(bundleName, typeof(SceneInstance));
            IList<IResourceLocation> locations;
            try
            {
                locations = new List<IResourceLocation>(await locationsHandle.ToUniTask());
            }
            finally
            {
                Addressables.Release(locationsHandle);
            }
            var completedProgress = 0f;
            foreach (var location in locations)
            {
                var assetId = location.PrimaryKey;
                if (_assetById.ContainsKey(assetId))
                {
                    _assetById[assetId].ContentKeysSet.Add(bundleName);
                    completedProgress++;
                    continue;
                }

                var loadHandle = Addressables.LoadSceneAsync(location, LoadSceneMode.Additive, activateOnLoad: true);
                var previousProgress = 0f;
                tasks.Add(loadHandle.ToUniTask(progress: Progress.Create<float>(value =>
                {
                    completedProgress += value - previousProgress;
                    previousProgress = value;
                    progress?.Report(completedProgress / locations.Count);
                })));

                _assetById[assetId] = new AssetModel()
                {
                    AssetId = assetId,
                    ContentKeysSet = new HashSet<string> { bundleName },
                    LoadHandle = loadHandle,
                    IsScene = true,
                };
            }

            await System.Threading.Tasks.Task.WhenAll(tasks.Select(task => task.AsTask()));
            progress?.Report(1f);
        }

        /// <summary>Releases in-memory resources for a completed load while leaving the disk cache intact. Must not run during loading.</summary>
        public void Release(string bundleName)
        {
            if (_isLoadingInProgress)
            {
                throw new InvalidOperationException("Wait for resource loading to finish before releasing content.");
            }
            var models = _assetById.Values.Where(x => x.ContentKeysSet.Contains(bundleName)).ToList();
            foreach (var model in models)
            {
                model.ContentKeysSet.Remove(bundleName);
                if (model.ContentKeysSet.Count > 0)
                {
                    continue;
                }
                if (model.LoadHandle.IsValid() && model.LoadHandle.Result is SpriteAtlas spriteAtlas)
                    RemoveSprites(spriteAtlas);
                
                _assetById.Remove(model.AssetId);
                if (model.IsScene && model.LoadHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    Addressables.UnloadSceneAsync(model.LoadHandle.Convert<SceneInstance>(), autoReleaseHandle: true);
                }
                else
                {
                    Addressables.Release(model.LoadHandle);
                }
            }
        }
        
        /// <summary>Registers sprites from the retained atlases for the existing presentation API.</summary>
        private void CollectSprites()
        {
            foreach (var assetModel in _assetById.Values)
            {
                if (assetModel.LoadHandle.Result is SpriteAtlas spriteAtlas)
                    CollectSprites(spriteAtlas);
            }
        }
        
        /// <summary>Copies atlas sprites into the name lookup used by presentation.</summary>
        private void CollectSprites(SpriteAtlas spriteAtlas)
        {
            if (_spritesByAtlas.ContainsKey(spriteAtlas))
            {
                return;
            }
            var count = spriteAtlas.spriteCount;
            var array = new Sprite[count];
            spriteAtlas.GetSprites(array);
            _spritesByAtlas.Add(spriteAtlas, new List<Sprite>(array));
            
            foreach (var sprite in array)
            {
                var spriteName = sprite.name.Replace("(Clone)", "");
                _sprites[spriteName] = sprite;
            }
        }
        
        /// <summary>Removes sprites supplied by an atlas whose final content owner is releasing it.</summary>
        private void RemoveSprites(SpriteAtlas spriteAtlas)
        {
            if (!_spritesByAtlas.Remove(spriteAtlas, out var spritesList))
            {
                return;
            }
            foreach (var sprite in spritesList)
            {
                var spriteName = sprite.name.Replace("(Clone)", "");
                if (_sprites.TryGetValue(spriteName, out var registeredSprite) && registeredSprite == sprite)
                {
                    _sprites.Remove(spriteName);
                }
                UnityEngine.Object.Destroy(sprite);
            }
        }

        /// <summary>Gets an already loaded asset or prefab/scene component; throws when it is missing or incompatible.</summary>
        public T GetAsset<T>(string assetId)
        {
            var model = _assetById.GetValueOrDefault(assetId);
            if (model == null)
                throw new Exception($"Missing asset with id <{assetId}>");
            
            if (model.LoadHandle.IsDone == false)
                throw new Exception($"Asset with id <{assetId}> not loaded yet");

            if (typeof(T) == typeof(Sprite))
                return (T)(object)GetSprite(assetId);

            var asset = model.LoadHandle.Result;

            if (model.IsScene)
                return GetComponent<T>(assetId, ((SceneInstance)asset).Scene.GetRootGameObjects().First());
            
            if (asset is GameObject gameObj && asset is not T)
                return GetComponent<T>(assetId, gameObj);
            
            if (asset is not T)
                throw new Exception($"Asset with id <{assetId}> is of type <{asset.GetType()}> but expected type is <{typeof(T)}>");
            
            return (T)asset;
        }

        /// <summary>Loads an additive scene; the caller owns its lifetime and must call UnloadSceneAsync.</summary>
        public async UniTask<SceneInstance> LoadSceneAsync(string sceneId)
        {
            var currentSceneOperation = Addressables.LoadSceneAsync(
                sceneId,
                LoadSceneMode.Additive,
                activateOnLoad: true
            );

            var sceneInstance = await currentSceneOperation.ToUniTask();
            return sceneInstance;
        }

        /// <summary>Unloads a caller-owned scene and releases the unload operation automatically on completion.</summary>
        public async UniTask UnloadSceneAsync(SceneInstance sceneInstance)
        {
            await Addressables.UnloadSceneAsync(sceneInstance, autoReleaseHandle: true).ToUniTask();
        }

        /// <summary>Returns a registered sprite, or logs and returns null when unavailable.</summary>
        private Sprite GetSprite(string assetId)
        {
            if (_sprites.TryGetValue(assetId, out var sprite))
            {
                return sprite;
            }
            else
            {
                Debug.LogWarning($"Missing sprite with id <{assetId}>");
                return null;
            }
        }
        
        /// <summary>Resolves the expected component on a loaded root, failing explicitly when the content is incompatible.</summary>
        private T GetComponent<T>(string assetId, GameObject gameObj)
        {
            var comp = gameObj.GetComponent<T>();
            if (comp == null)
                throw new Exception($"Asset with id <{assetId}> missing component <{typeof(T)}>");
            else
                return comp;
        }
    }
}
