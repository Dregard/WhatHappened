using System;
using System.Collections.Generic;
using System.Linq;
using Game.Levels;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.AssetManagement.Editor
{
    public static class LevelContentBuildController
    {
        /// <summary>Configures remote level bundles and catalogs, preserving an authored Remote.LoadPath. Extracts shared dependencies without moving source assets.</summary>
        [MenuItem("Game/Content/Configure Remote Levels")]
        public static void Configure()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new InvalidOperationException("Addressables settings are required.");
            }
            var levelsGroup = settings.FindGroup("Levels");
            if (levelsGroup == null)
            {
                throw new InvalidOperationException("The Levels Addressables group is required.");
            }
            var profileId = settings.profileSettings.GetProfileId("Remote");
            if (string.IsNullOrEmpty(profileId))
            {
                throw new InvalidOperationException("The authored Remote Addressables profile is required.");
            }
            var remotePath = settings.profileSettings.GetValueByName(profileId, "Remote.LoadPath");
            var buildPath = settings.profileSettings.GetValueByName(profileId, "Remote.BuildPath");
            if (string.IsNullOrWhiteSpace(remotePath) || remotePath == "<undefined>" ||
                string.IsNullOrWhiteSpace(buildPath) || buildPath == "<undefined>")
            {
                throw new InvalidOperationException("Configure the project's Remote.LoadPath and Remote.BuildPath before preparing levels.");
            }
            settings.activeProfileId = profileId;
            settings.BuildRemoteCatalog = true;
            // The player includes the catalog, so offline startup does not require a remote catalog request.
            settings.DisableCatalogUpdateOnStartup = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, "Remote.BuildPath");
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, "Remote.LoadPath");
            ConfigureRemoteGroup(settings, levelsGroup);
            RegisterLevels(settings, levelsGroup);
            ExtractSharedDependencies(settings, levelsGroup);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"Remote levels configured. Build path: {buildPath}; load path: {remotePath}.");
        }

        /// <summary>Registers all authored level data, preserving existing addresses referenced by progression and editor selectors.</summary>
        private static void RegisterLevels(AddressableAssetSettings settings, AddressableAssetGroup levelsGroup)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:LevelData", new[] { "Assets" }))
            {
                var entry = settings.FindAssetEntry(guid);
                var address = entry == null ? AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid)).name : entry.address;
                if (string.IsNullOrWhiteSpace(address) || settings.groups.Where(group => group != null)
                    .SelectMany(group => group.entries).Any(other => other.guid != guid && other.address == address))
                {
                    throw new InvalidOperationException($"Level '{AssetDatabase.GUIDToAssetPath(guid)}' requires a unique nonempty address: '{address}'.");
                }
                entry = settings.CreateOrMoveEntry(guid, levelsGroup);
                entry.SetAddress(address);
                entry.SetLabel("Level", true, true);
            }
        }

        /// <summary>Builds the active platform's content using authored settings and fails explicitly on Addressables errors.</summary>
        [MenuItem("Game/Content/Build Remote Levels")]
        public static void Build()
        {
            Configure();
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new InvalidOperationException(result.Error);
            }
            Debug.Log($"Remote level content build succeeded: {result.OutputPath}");
        }

        /// <summary>Uses separate cached bundles per entry, retaining independently authored compression and other settings.</summary>
        private static void ConfigureRemoteGroup(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
            {
                schema = group.AddSchema<BundledAssetGroupSchema>();
            }
            schema.UseDefaultSchemaSettings = false;
            schema.IncludeInBuild = true;
            schema.IncludeAddressInCatalog = true;
            schema.IncludeLabelsInCatalog = true;
            schema.BuildPath.SetVariableByName(settings, "Remote.BuildPath");
            schema.LoadPath.SetVariableByName(settings, "Remote.LoadPath");
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            schema.UseAssetBundleCache = true;
            schema.AssetBundledCacheClearBehavior = BundledAssetGroupSchema.CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
            EditorUtility.SetDirty(schema);
        }

        /// <summary>
        /// Promotes implicit dependencies used by multiple level/local roots to explicit entries to avoid bundle duplication.
        /// Dependencies also needed by local content stay local; remote-only shared entries use independent remote bundles.
        /// Existing explicit entries, GUIDs, level addresses and source files are preserved.
        /// </summary>
        private static void ExtractSharedDependencies(AddressableAssetSettings settings, AddressableAssetGroup levelsGroup)
        {
            var dependencyUseCountsByPath = new Dictionary<string, int>();
            var localDependenciesSet = new HashSet<string>();
            var rootsArray = settings.groups.Where(group => group != null && group.Name != "Level Shared")
                .SelectMany(group => group.entries).ToArray();
            foreach (var entry in rootsArray)
            {
                var dependenciesArray = AssetDatabase.GetDependencies(entry.AssetPath, true);
                foreach (var path in dependenciesArray)
                {
                    if (path == entry.AssetPath || !path.StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                    if (assetType == typeof(MonoScript) || assetType == null || AssetDatabase.IsValidFolder(path))
                    {
                        continue;
                    }
                    dependencyUseCountsByPath.TryGetValue(path, out var count);
                    dependencyUseCountsByPath[path] = count + 1;
                    if (entry.parentGroup != levelsGroup)
                    {
                        localDependenciesSet.Add(path);
                    }
                }
            }
            var sharedGroup = settings.FindGroup("Level Shared");
            foreach (var pair in dependencyUseCountsByPath)
            {
                if (pair.Value < 2)
                {
                    continue;
                }
                var guid = AssetDatabase.AssetPathToGUID(pair.Key);
                if (settings.FindAssetEntry(guid) != null)
                {
                    continue;
                }
                AddressableAssetGroup targetGroup;
                if (localDependenciesSet.Contains(pair.Key))
                {
                    targetGroup = settings.DefaultGroup;
                }
                else
                {
                    if (sharedGroup == null)
                    {
                        sharedGroup = settings.CreateGroup("Level Shared", false, false, false, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                    }
                    targetGroup = sharedGroup;
                }
                settings.CreateOrMoveEntry(guid, targetGroup).address = pair.Key;
            }
            if (sharedGroup != null)
            {
                ConfigureRemoteGroup(settings, sharedGroup);
            }
        }
    }
}
