#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Game.Gameplay;

namespace Game.Levels.Editor
{
    [CustomPropertyDrawer(typeof(LevelReference))]
    public class LevelReferenceDrawer : PropertyDrawer
    {
        private class LevelOption
        {
            public string Address;
            public string DisplayName;
        }

        private List<LevelOption> _options;
        private string[] _displayNames;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_options == null)
            {
                RefreshOptions();
            }

            var levelIdProp = property.FindPropertyRelative("LevelId");
            var currentId = levelIdProp.stringValue;

            var currentIndex = -1;
            if (_options != null)
            {
                for (var i = 0; i < _options.Count; i++)
                {
                    if (_options[i].Address == currentId)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            if (_options == null || _options.Count == 0)
            {
                EditorGUI.LabelField(position, "No levels in 'Levels' group");
            }
            else
            {
                var nextIndex = EditorGUI.Popup(position, currentIndex, _displayNames);
                if (nextIndex != currentIndex)
                {
                    levelIdProp.stringValue = _options[nextIndex].Address;
                }
            }

            EditorGUI.EndProperty();
        }

        private void RefreshOptions()
        {
            _options = new List<LevelOption>();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var group = settings.FindGroup("Levels");
            if (group == null) return;

            foreach (var entry in group.entries)
            {
                var path = AssetDatabase.GUIDToAssetPath(entry.guid);
                var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);

                if (levelData != null)
                {
                    _options.Add(new LevelOption
                    {
                        Address = entry.address,
                        DisplayName = levelData.name
                    });
                }
                else
                {
                    _options.Add(new LevelOption
                    {
                        Address = entry.address,
                        DisplayName = $"{entry.address} (Invalid Data)"
                    });
                }
            }

            _displayNames = _options.Select(o => o.DisplayName).ToArray();
        }
    }
}
#endif
