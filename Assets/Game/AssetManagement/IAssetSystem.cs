using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Game.AssetManagement
{
    public interface IAssetSystem
    {
        /// <summary>Gets an already loaded asset; its owner must retain the corresponding content until consumers finish.</summary>
        public T GetAsset<T>(string assetId);
        /// <summary>Downloads dependencies to disk without loading assets; optionally reports byte progress. Completes when the cache is ready, or throws on failure.</summary>
        public UniTask DownloadAsync(string contentKey, System.IProgress<float> progress = null);
        /// <summary>Returns bytes still missing from disk without loading assets.</summary>
        public UniTask<long> GetDownloadSizeAsync(string contentKey);
        /// <summary>Deletes unloaded level bundles unused by retained keys and loaded content. A null retained set protects every other catalog level.</summary>
        public UniTask<bool> RemoveDownloadedLevelAsync(string levelId, IEnumerable<string> retainedLevelIdsEnumerable = null);
        /// <summary>Loads and activates an additive scene; the caller owns it until UnloadSceneAsync completes.</summary>
        public UniTask<SceneInstance> LoadSceneAsync(string sceneId);
        /// <summary>Unloads the scene and releases its Addressables ownership when the asynchronous operation completes.</summary>
        public UniTask UnloadSceneAsync(SceneInstance sceneInstance);
    }
}
