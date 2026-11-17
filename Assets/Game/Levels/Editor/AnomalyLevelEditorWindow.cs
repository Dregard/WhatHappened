using System.Collections.Generic;
using Game.Anomalies;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Game.Levels.Editor
{
    public sealed class AnomalyLevelEditorWindow : EditorWindow
    {
        [SerializeField, Tooltip("Level asset currently being edited; restored after editor reload.")]
        private LevelData _level;
        private SerializedObject _serializedLevel;
        private Vector2 _scroll;
        private Vector2 _warningsScroll;
        private int _selectedIndex = -1;
        private int _previewStage = -1;
        private int _seed;
        private bool _needsValidation = true;
        private readonly AnomalyImagePreview _preview = new();
        private List<IReadOnlyList<AnomalyData>> _stagesList = new();
        private List<string> _warningsList = new();

        /// <summary>Opens the supplied asset without creating duplicate content or changing progression.</summary>
        public static void Open(LevelData level)
        {
            var window = GetWindow<AnomalyLevelEditorWindow>("Anomaly Level");
            window._level = level;
            window._serializedLevel = null;
            window._selectedIndex = -1;
            window._previewStage = -1;
            window._needsValidation = true;
            window.minSize = new Vector2(850, 550);
            window.Show();
        }

        /// <summary>Routes double-clicks on level assets to whole-image authoring, including legacy levels.</summary>
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not LevelData level)
                return false;
            Open(level);
            return true;
        }

        /// <summary>Refreshes serialized state after undo and external Inspector edits.</summary>
        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.projectChanged += OnUndo;
        }

        /// <summary>Releases editor callbacks; asset edits remain dirty for Unity's normal save flow.</summary>
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.projectChanged -= OnUndo;
        }

        /// <summary>Invalidates preview diagnostics without modifying authored values.</summary>
        private void OnUndo()
        {
            _needsValidation = true;
            Repaint();
        }

        /// <summary>Draws serialized authoring controls alongside a fitted, draggable image composition.</summary>
        private void OnGUI()
        {
            var level = (LevelData)EditorGUILayout.ObjectField("Level", _level, typeof(LevelData), false);
            if (level != _level)
            {
                _level = level;
                _serializedLevel = null;
                _selectedIndex = -1;
                _needsValidation = true;
            }
            if (_level == null)
                return;
            _serializedLevel ??= new SerializedObject(_level);
            _serializedLevel.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(340));
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawProperties();
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Save Level"))
            {
                if (_serializedLevel.ApplyModifiedProperties())
                    _needsValidation = true;
                AssetDatabase.SaveAssetIfDirty(_level);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            if (_serializedLevel.ApplyModifiedProperties())
                _needsValidation = true;
            DrawPreview();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Exposes search data directly; no puzzle slicing or automatic count/ID correction is performed.</summary>
        private void DrawProperties()
        {
            EditorGUILayout.PropertyField(_serializedLevel.FindProperty("LevelTime"));
            var search = _serializedLevel.FindProperty("SearchData");
            EditorGUILayout.PropertyField(search.FindPropertyRelative("MainImage"));
            EditorGUILayout.PropertyField(search.FindPropertyRelative("ClueImage"));
            DrawAnomalies(search.FindPropertyRelative("AnomaliesList"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stages (in play order)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Count is the number of slots. Empty IDs are random. IDs cannot repeat across stages. Explicit future IDs are reserved.", MessageType.Info);
            EditorGUILayout.PropertyField(search.FindPropertyRelative("StagesList"), true);
            EditorGUILayout.PropertyField(search.FindPropertyRelative("FindingDuration"));
            EditorGUILayout.PropertyField(search.FindPropertyRelative("TransitionDuration"));
            EditorGUILayout.PropertyField(search.FindPropertyRelative("Presentation"), true);
        }

        /// <summary>Edits an arbitrary list of complete sprite layers, preserving their IDs, text and normalized rectangles.</summary>
        private void DrawAnomalies(SerializedProperty anomalies)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Anomaly images", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select a layer, then drag it on the picture. Bounds use normalized coordinates with bottom-left origin. Edit Bounds for precise placement/size. Undo is supported.", MessageType.Info);
            for (var i = 0; i < anomalies.arraySize; i++)
            {
                var anomaly = anomalies.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                var id = anomaly.FindPropertyRelative("Id").stringValue;
                if (GUILayout.Toggle(_selectedIndex == i, $"{i + 1}. {id}", "Button"))
                    _selectedIndex = i;
                if (GUILayout.Button("Remove", GUILayout.Width(65)))
                {
                    anomalies.DeleteArrayElementAtIndex(i);
                    _selectedIndex = -1;
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
                if (_selectedIndex != i)
                    continue;
                EditorGUILayout.PropertyField(anomaly.FindPropertyRelative("Id"));
                EditorGUILayout.PropertyField(anomaly.FindPropertyRelative("Image"));
                EditorGUILayout.PropertyField(anomaly.FindPropertyRelative("Bounds"));
                EditorGUILayout.PropertyField(anomaly.FindPropertyRelative("HudText"));
            }
            if (GUILayout.Button("Add Anomaly"))
            {
                var index = anomalies.arraySize++;
                var anomaly = anomalies.GetArrayElementAtIndex(index);
                anomaly.FindPropertyRelative("Id").stringValue = string.Empty;
                anomaly.FindPropertyRelative("Image").objectReferenceValue = null;
                anomaly.FindPropertyRelative("Bounds").rectValue = new Rect(0.4f, 0.4f, 0.2f, 0.2f);
                anomaly.FindPropertyRelative("HudText").stringValue = string.Empty;
                _selectedIndex = index;
            }
        }

        /// <summary>Shows structural errors and a repeatable runtime selection, retaining all original author values.</summary>
        private void DrawPreview()
        {
            var data = _level.SearchData;
            if (data == null)
                return;
            EditorGUI.BeginChangeCheck();
            _seed = EditorGUILayout.IntField("Selection preview seed", _seed);
            if (EditorGUI.EndChangeCheck())
                _needsValidation = true;
            if (GUILayout.Button("Refresh Warnings"))
                _needsValidation = true;
            if (_needsValidation)
            {
                _warningsList = AnomalyAuthoringValidation.Validate(data);
                _stagesList = AnomalyStageSelectionController.Select(data, new System.Random(_seed), _warningsList);
                _needsValidation = false;
            }
            var optionsArray = new string[_stagesList.Count + 1];
            optionsArray[0] = "All layers (composition)";
            for (var i = 0; i < _stagesList.Count; i++)
                optionsArray[i + 1] = $"Stage {i + 1}: {_stagesList[i].Count} selected";
            _previewStage = EditorGUILayout.Popup("Preview", Mathf.Clamp(_previewStage + 1, 0, optionsArray.Length - 1), optionsArray) - 1;
            EditorGUILayout.LabelField("Stage preview includes previous-stage exclusions. Random outcomes vary with seed.", EditorStyles.wordWrappedMiniLabel);
            if (_warningsList.Count > 0)
            {
                _warningsScroll = EditorGUILayout.BeginScrollView(_warningsScroll, GUILayout.Height(120));
                foreach (var warning in _warningsList)
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                EditorGUILayout.EndScrollView();
            }
            var area = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            IReadOnlyList<AnomalyData> visibleList = _previewStage < 0 ? data.AnomaliesList : _stagesList[_previewStage];
            if (_preview.Draw(area, data.MainImage, data.AnomaliesList, visibleList, ref _selectedIndex, _level))
            {
                _needsValidation = true;
                Repaint();
            }
        }
    }
}
