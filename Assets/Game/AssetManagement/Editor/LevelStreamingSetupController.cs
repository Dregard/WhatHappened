using System;
using System.Linq;
using Game.Levels;
using Game.UI.Buttons;
using Game.UI.Windows.Controllers;
using Game.UI.Windows.DownloadWaitWindow;
using Game.UI.Windows.General;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AssetManagement.Editor
{
    public static class LevelStreamingSetupController
    {
        private const string WindowPath = "Assets/Game/UI/Windows/DownloadWaitWindow/DownloadWaitWindow.prefab";

        /// <summary>Creates the waiting-window variant, registers it and migrates the legacy loop range once, without modifying loaded scenes.</summary>
        [MenuItem("Game/Content/Configure Level Streaming")]
        public static void Configure()
        {
            var window = AssetDatabase.LoadAssetAtPath<DownloadWaitWindow>(WindowPath);
            var isFirstSetup = window == null;
            if (isFirstSetup)
            {
                CreateWindow();
                window = AssetDatabase.LoadAssetAtPath<DownloadWaitWindow>(WindowPath);
            }
            var containerPath = "Assets/Game/UI/Windows/WindowsContainer.prefab";
            var root = PrefabUtility.LoadPrefabContents(containerPath);
            try
            {
                var serializedSystem = new SerializedObject(root.GetComponent<WindowSystem>());
                var prefabs = serializedSystem.FindProperty("_windowPrefabs");
                var isRegistered = Enumerable.Range(0, prefabs.arraySize)
                    .Any(index => prefabs.GetArrayElementAtIndex(index).objectReferenceValue == window);
                if (!isRegistered)
                {
                    var index = prefabs.arraySize++;
                    prefabs.GetArrayElementAtIndex(index).objectReferenceValue = window;
                    serializedSystem.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, containerPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            var levels = AssetDatabase.LoadAssetAtPath<LevelsData>("Assets/Game/Levels/LevelsData.asset");
            if (isFirstSetup)
            {
                var serializedLevels = new SerializedObject(levels);
                serializedLevels.FindProperty("_loopLevelCount").intValue = Math.Max(1, levels.Levels.Length - Math.Max(1, levels.LoopStartLevel) + 1);
                serializedLevels.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(levels);
                AssetDatabase.SaveAssetIfDirty(levels);
            }
            Debug.Log($"Level streaming configured: reserve={levels.DownloadReserveCount}, loop={levels.LoopLevelCount}, shuffled={levels.RandomizeLoopOrder}. Waiting window registered.");
        }

        /// <summary>Builds a variant using the existing window controller, font and game button. The preview scene is always closed.</summary>
        private static void CreateWindow()
        {
            var previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Windows/General/WindowBase.prefab");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, previewScene);
                root.name = "DownloadWaitWindow";
                var view = root.AddComponent<DownloadWaitWindow>();
                var serializedController = new SerializedObject(root.GetComponent<WindowController>());
                serializedController.FindProperty("_canCloseByReturn").boolValue = false;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                var panel = new GameObject("ConnectionPanel", typeof(RectTransform), typeof(Image));
                panel.layer = root.layer;
                var panelRect = (RectTransform)panel.transform;
                panelRect.SetParent(root.transform, false);
                panelRect.anchorMin = new Vector2(0.1f, 0.35f);
                panelRect.anchorMax = new Vector2(0.9f, 0.65f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                panel.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 1f);

                var textRoot = new GameObject("ConnectionMessage", typeof(RectTransform), typeof(TextMeshProUGUI));
                textRoot.layer = root.layer;
                var text = textRoot.GetComponent<TextMeshProUGUI>();
                text.rectTransform.SetParent(panelRect, false);
                text.rectTransform.anchorMin = new Vector2(0.08f, 0.45f);
                text.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
                text.rectTransform.offsetMin = Vector2.zero;
                text.rectTransform.offsetMax = Vector2.zero;
                var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Windows/PauseWindow/PauseWindow.prefab");
                text.font = pausePrefab.GetComponentInChildren<TMP_Text>(true).font;
                text.text = "Connect to the internet to download the next level.\nThen tap Retry.";
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 20;
                text.fontSizeMax = 48;
                text.raycastTarget = false;

                var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/Buttons/Button.prefab");
                var buttonRoot = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, previewScene);
                buttonRoot.name = "RetryButton";
                var buttonRect = (RectTransform)buttonRoot.transform;
                buttonRect.SetParent(panelRect, false);
                buttonRect.anchorMin = new Vector2(0.25f, 0.1f);
                buttonRect.anchorMax = new Vector2(0.75f, 0.35f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
                var buttonText = buttonRoot.GetComponentInChildren<TMP_Text>(true);
                buttonText.text = "Retry";
                var serializedView = new SerializedObject(view);
                serializedView.FindProperty("_retryButton").objectReferenceValue = buttonRoot.GetComponent<GameButton>();
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }
    }
}
