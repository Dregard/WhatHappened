#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using Game.Initialization;

namespace Game.Levels.Editor
{
    [CustomEditor(typeof(LevelChooser))]
    public class LevelChooserEditor : UnityEditor.Editor
    {
        private LevelChooser _chooser;
        private string[] _levelAddresses;
        private int _selectedIndex = -1;

        
        private void OnEnable()
        {
            _chooser = (LevelChooser)target;
            LoadLevelAddresses();
            UpdateSelectedIndex();
            
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            SaveCurrentLevelChanges();
        }

        private void LoadLevelAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                _levelAddresses = new string[0];
                return;
            }

            var group = settings.FindGroup("Levels");
            if (group == null)
            {
                _levelAddresses = new string[0];
                return;
            }

            _levelAddresses = group.entries.Select(e => e.address).ToArray();
        }

        private void UpdateSelectedIndex()
        {
            if (string.IsNullOrEmpty(_chooser.SelectedLevelAddress))
            {
                _selectedIndex = -1;
                return;
            }

            _selectedIndex = System.Array.IndexOf(_levelAddresses, _chooser.SelectedLevelAddress);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_levelAddresses == null || _levelAddresses.Length == 0)
            {
                EditorGUILayout.HelpBox("No levels found in Addressables Group 'Levels'", MessageType.Warning);
                if (GUILayout.Button("Refresh"))
                {
                    LoadLevelAddresses();
                }
                return;
            }

            var newIndex = EditorGUILayout.Popup("Select Level", _selectedIndex, _levelAddresses);

            if (newIndex != _selectedIndex)
            {
                ChangeLevel(newIndex);
            }

            if (GUILayout.Button("Save Changes (Apply Prefab)"))
            {
                SaveCurrentLevelChanges();
            }

            if (GUILayout.Button("Refresh List"))
            {
                LoadLevelAddresses();
                UpdateSelectedIndex();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ChangeLevel(int newIndex)
        {
            SaveCurrentLevelChanges();

            if (_chooser.CurrentLevelInstance != null)
            {
                DestroyImmediate(_chooser.CurrentLevelInstance);
                _chooser.CurrentLevelInstance = null;
            }

            _selectedIndex = newIndex;
            if (_selectedIndex >= 0 && _selectedIndex < _levelAddresses.Length)
            {
                _chooser.SelectedLevelAddress = _levelAddresses[_selectedIndex];
                InstantiateSelectedLevel();
            }
            else
            {
                _chooser.SelectedLevelAddress = string.Empty;
            }
            
            EditorUtility.SetDirty(_chooser);
        }

        private void InstantiateSelectedLevel()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup("Levels");
            var entry = group.entries.FirstOrDefault(e => e.address == _chooser.SelectedLevelAddress);

            if (entry == null)
            {
                Debug.LogError($"Level with address '{_chooser.SelectedLevelAddress}' not found in Addressables.");
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(entry.guid);
            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);

            if (levelData == null)
            {
                Debug.LogError($"Could not load LevelData from path: {path}");
                return;
            }

            var gameplayInstaller = FindObjectOfType<GameplayInstaller>();
            if (gameplayInstaller == null)
            {
                Debug.LogError("GameplayInstaller not found in scene.");
                return;
            }

            // В редакторе мы все еще можем хотеть видеть визуализацию, 
            // но теперь мы берем префаб контроллера из инсталлера или просто работаем с тем что есть.
            // Однако, по ТЗ мы удаляем LevelController из каждого уровня.
            // Если нам нужно превью в редакторе, это отдельная задача, 
            // но сейчас мы просто исправляем поломку.
            
            Debug.Log($"Level {levelData.name} selected. LevelController is now global.");
        }

        private void SaveCurrentLevelChanges()
        {
            if (_chooser.CurrentLevelInstance == null) return;

            if (PrefabUtility.IsPartOfPrefabInstance(_chooser.CurrentLevelInstance))
            {
                PrefabUtility.ApplyPrefabInstance(_chooser.CurrentLevelInstance, InteractionMode.AutomatedAction);
                Debug.Log($"Changes saved to prefab for: {_chooser.SelectedLevelAddress}");
            }
        }
    }
}
#endif
