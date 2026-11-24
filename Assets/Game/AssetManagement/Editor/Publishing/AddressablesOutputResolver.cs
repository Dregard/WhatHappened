using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Reads remote output locations from the active Addressables profile and files from the finished build. Never builds content.</summary>
    public static class AddressablesOutputResolver
    {
        /// <summary>
        /// Collects evaluated build/load paths of remote groups included in the build and of the remote catalog, plus the file
        /// registry of the supplied result. Paths come from the active profile; RemoteLoadPath values are not modified.
        /// </summary>
        public static AddressablesOutputData Resolve(AddressableAssetSettings settings, AddressablesPlayerBuildResult result)
        {
            if (settings == null || result == null || result.FileRegistry == null)
            {
                throw new RemoteContentPublishException(PublishStageAlias.Configuration, "Нет результата сборки Addressables с реестром файлов текущего запуска.");
            }
            var locationsList = new List<OutputLocationData>();
            foreach (var group in settings.groups.Where(candidate => candidate != null))
            {
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema != null && schema.IncludeInBuild)
                {
                    AddLocation(locationsList, schema.BuildPath.GetValue(settings), schema.LoadPath.GetValue(settings));
                }
            }
            if (settings.BuildRemoteCatalog)
            {
                AddLocation(locationsList, settings.RemoteCatalogBuildPath.GetValue(settings), settings.RemoteCatalogLoadPath.GetValue(settings));
            }
            return new AddressablesOutputData(locationsList, result.FileRegistry.GetFilePaths().ToList());
        }

        /// <summary>Returns the evaluated Remote.LoadPath of the active profile for display purposes; empty when undefined.</summary>
        public static string GetRemoteLoadPath(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                return string.Empty;
            }
            var value = settings.profileSettings.GetValueByName(settings.activeProfileId, "Remote.LoadPath");
            return string.IsNullOrEmpty(value) ? string.Empty : settings.profileSettings.EvaluateString(settings.activeProfileId, value);
        }

        /// <summary>Adds a location only when its load path is an HTTP(S) URL (remote) and its build folder is not yet registered.</summary>
        private static void AddLocation(List<OutputLocationData> locationsList, string buildPath, string loadPath)
        {
            var isRemote = !string.IsNullOrEmpty(loadPath) && (loadPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || loadPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
            if (!isRemote || string.IsNullOrWhiteSpace(buildPath))
            {
                return;
            }
            var directory = RemoteContentPlanBuilder.NormalizePath(buildPath);
            if (!locationsList.Any(existing => string.Equals(existing.LocalDirectory, directory, StringComparison.OrdinalIgnoreCase)))
            {
                locationsList.Add(new OutputLocationData(directory, loadPath));
            }
        }
    }
}
