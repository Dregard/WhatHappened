using System.Collections.Generic;
using Game.Anomalies;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.Editor
{
    [CustomEditor(typeof(LevelData))]
    public sealed class LevelDataEditor : UnityEditor.Editor
    {
        private string _warnings;

        /// <summary>Refreshes authoring warnings after edits; selection validation uses a stable seed and never changes the asset.</summary>
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Anomaly Level Editor"))
                AnomalyLevelEditorWindow.Open((LevelData)target);
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            var changed = EditorGUI.EndChangeCheck();
            if (changed || _warnings == null)
            {
                var data = ((LevelData)target).SearchData;
                var warningsList = data == null ? new List<string>() : AnomalyAuthoringValidation.Validate(data);
                if (data?.MainImage != null)
                    AnomalyStageSelectionController.Select(data, new System.Random(0), warningsList);
                else
                    warningsList.Add("Assign SearchData.MainImage before adding this level to progression.");
                _warnings = string.Join("\n", warningsList);
            }
            if (!string.IsNullOrEmpty(_warnings))
                EditorGUILayout.HelpBox(_warnings, MessageType.Warning);
        }
    }
}
