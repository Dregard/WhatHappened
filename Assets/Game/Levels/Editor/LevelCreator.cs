#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Game.AssetManagement.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.Levels.Editor
{
    public static class LevelCreator
    {
        private const string LevelsGroupName = "Levels";
        private const string LevelsFolder = "Assets/Game/Levels";

        /// <summary>Creates remote anomaly-level content and opens its whole-image authoring window.</summary>
        [MenuItem("Game/Create Level")]
        public static void CreateLevel()
        {
            var levelData = CreateLevelData(LevelsFolder);
            Selection.activeObject = levelData;
            AnomalyLevelEditorWindow.Open(levelData);
        }

        /// <summary>Creates a level asset with empty search configuration and persists remote registration without changing progression order.</summary>
        private static LevelData CreateLevelData(string parentFolder)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new System.InvalidOperationException("AddressableAssetSettings not found. Please create it first.");
            }

            LevelContentBuildController.Configure();
            var group = settings.FindGroup(LevelsGroupName);

            var levelCount = group.entries.Count;
            var levelName = $"Level{levelCount}";
            while (AssetDatabase.IsValidFolder($"{parentFolder}/{levelName}") ||
                settings.groups.Where(candidate => candidate != null).SelectMany(candidate => candidate.entries)
                    .Any(entry => entry.address == levelName))
            {
                levelName = $"Level{++levelCount}";
            }
            var levelFolder = $"{parentFolder}/{levelName}";

            if (!Directory.Exists(levelFolder))
            {
                Directory.CreateDirectory(levelFolder);
                AssetDatabase.Refresh();
            }

            var levelData = ScriptableObject.CreateInstance<LevelData>();
            
            var configPath = $"{levelFolder}/{levelName}.asset";
            AssetDatabase.CreateAsset(levelData, configPath);
            
            AssetDatabase.SaveAssets();

            LevelContentBuildController.Configure();

            Debug.Log($"Created new level: {levelName} at {configPath}");
            return levelData;
        }

    }
}
#endif
