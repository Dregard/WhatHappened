using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Game.AssetManagement
{
    public class AddressablesCacheController
    {
        /// <summary>Returns missing download bytes without loading assets; owns and releases the query handle.</summary>
        public async UniTask<long> GetDownloadSizeAsync(string contentKey)
        {
            var handle = Addressables.GetDownloadSizeAsync(contentKey);
            try
            {
                return await handle.ToUniTask();
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        /// <summary>Downloads dependencies without loading assets/scenes; reports real byte progress and cached completion. Releases the operation on completion or failure.</summary>
        public async UniTask DownloadAsync(string contentKey, IProgress<float> progress = null)
        {
            if (await GetDownloadSizeAsync(contentKey) == 0)
            {
                progress?.Report(1f);
                return;
            }
            var locationsList = await GetLocationsAsync(contentKey);
            var handle = Addressables.DownloadDependenciesAsync(locationsList, autoReleaseHandle: false);
            try
            {
                await handle.ToUniTask(progress: Progress.Create<float>(_ => progress?.Report(handle.GetDownloadStatus().Percent)));
                progress?.Report(1f);
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Removes only cached bundle versions exclusive to the target. Callers must serialize cache mutations
        /// with loads/downloads and supply every retained or loaded content key. Returns false if Unity cannot clear a version.
        /// Metadata queries do not load assets. Shared versions and other versions of a bundle remain intact.
        /// </summary>
        public async UniTask<bool> RemoveAsync(string contentKey, IEnumerable<string> retainedKeysEnumerable)
        {
            var targetLocationsList = await GetLocationsAsync(contentKey);
            var targetBundlesSet = new HashSet<CachedAssetBundle>();
            CollectBundles(targetLocationsList, targetBundlesSet);
            var retainedBundlesSet = new HashSet<CachedAssetBundle>();
            var retainedKeysSet = new HashSet<string>(retainedKeysEnumerable);
            foreach (var retainedKey in retainedKeysSet)
            {
                CollectBundles(await GetLocationsAsync(retainedKey), retainedBundlesSet);
            }

            targetBundlesSet.ExceptWith(retainedBundlesSet);
            if (!Caching.ready)
            {
                throw new InvalidOperationException("Unity's bundle cache is not ready.");
            }

            var isSuccessful = true;
            foreach (var bundle in targetBundlesSet)
            {
                if (Caching.IsVersionCached(bundle))
                {
                    isSuccessful &= Caching.ClearCachedVersion(bundle.name, bundle.hash);
                }
            }
            return isSuccessful;
        }

        /// <summary>Copies catalog locations before releasing their handle; unknown keys fail rather than silently succeeding.</summary>
        private async UniTask<List<IResourceLocation>> GetLocationsAsync(string contentKey)
        {
            var handle = Addressables.LoadResourceLocationsAsync(contentKey);
            try
            {
                var locationsList = await handle.ToUniTask();
                if (locationsList.Count == 0)
                {
                    throw new ArgumentException($"No Addressables content found for '{contentKey}'.", nameof(contentKey));
                }
                return new List<IResourceLocation>(locationsList);
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        /// <summary>Traverses the full dependency graph once per query, collecting exact cached bundle identities.</summary>
        private void CollectBundles(IEnumerable<IResourceLocation> locationsEnumerable, HashSet<CachedAssetBundle> bundlesSet)
        {
            var locationsStack = new Stack<IResourceLocation>(locationsEnumerable);
            var visitedLocationsSet = new HashSet<IResourceLocation>();
            while (locationsStack.Count > 0)
            {
                var location = locationsStack.Pop();
                if (!visitedLocationsSet.Add(location))
                {
                    continue;
                }
                if (location.Data is AssetBundleRequestOptions options && !string.IsNullOrEmpty(options.Hash))
                {
                    bundlesSet.Add(new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash)));
                }
                if (location.HasDependencies)
                {
                    foreach (var dependency in location.Dependencies)
                    {
                        locationsStack.Push(dependency);
                    }
                }
            }
        }
    }
}
