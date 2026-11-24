using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Project Settings page: publication settings, Build Profile opt-in, local secrets and the «Проверить подключение» button.</summary>
    internal sealed class RemoteContentPublishSettingsView
    {
        private const string ProfilesPropertyName = "_enabledProfileGuidsList";

        private readonly RemoteContentSecretStore _secrets = new RemoteContentSecretStore();
        private SerializedObject _serialized;
        private string _passwordInput = string.Empty;
        private string _fingerprintInput = string.Empty;
        private string _statusText = string.Empty;
        private MessageType _statusType = MessageType.None;

        /// <summary>Registers the page in Project Settings > Remote Content Publishing.</summary>
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var view = new RemoteContentPublishSettingsView();
            return new SettingsProvider("Project/Remote Content Publishing", SettingsScope.Project)
            {
                label = "Remote Content Publishing",
                guiHandler = view.Draw,
                keywords = new[] { "Addressables", "SFTP", "publish", "remote content" }
            };
        }

        /// <summary>Draws the whole page and saves settings when a project field changed.</summary>
        private void Draw(string searchContext)
        {
            var store = RemoteContentPublishProjectData.instance;
            if (_serialized == null || _serialized.targetObject == null)
            {
                _serialized = new SerializedObject(store);
            }
            _serialized.Update();
            EditorGUI.BeginChangeCheck();
            DrawFields();
            DrawProfiles();
            if (EditorGUI.EndChangeCheck())
            {
                _serialized.ApplyModifiedProperties();
                store.SaveSettings();
            }
            DrawSecrets();
            DrawConnectionCheck(store.Data);
        }

        /// <summary>Draws every serialized setting except the profile list, and the destination hint.</summary>
        private void DrawFields()
        {
            var dataProperty = _serialized.FindProperty(RemoteContentPublishProjectData.DataFieldName);
            var iterator = dataProperty.Copy();
            var end = dataProperty.GetEndProperty();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                if (iterator.name != ProfilesPropertyName)
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
                if (iterator.name == "_destinationRoot")
                {
                    EditorGUILayout.HelpBox("Ориентир из панели хостинга: " + BuildHint() + "\nИсточник истины — путь из строки «Удалённый сайт» FileZilla, открытой в папке RemoteContent. Если он отличается, впишите путь FileZilla.", MessageType.Info);
                }
            }
        }

        /// <summary>Builds the hosting-panel hint, taking the project folder from the active Remote.LoadPath when possible.</summary>
        private static string BuildHint()
        {
            var loadPath = AddressablesOutputResolver.GetRemoteLoadPath(AddressableAssetSettingsDefaultObject.Settings);
            var name = RemoteContentPathMapper.TryGetProjectFolderName(loadPath, out var folder) ? folder : "{имя проекта}";
            return "/www/sunstormstudio.com/RemoteContent/" + name;
        }

        /// <summary>Draws one toggle per Build Profile asset; only toggled profiles publish automatically.</summary>
        private void DrawProfiles()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Автопубликация для Build Profile", EditorStyles.boldLabel);
            var listProperty = _serialized.FindProperty(RemoteContentPublishProjectData.DataFieldName).FindPropertyRelative(ProfilesPropertyName);
            var guidsArray = AssetDatabase.FindAssets("t:BuildProfile");
            if (guidsArray.Length == 0)
            {
                EditorGUILayout.HelpBox("Build Profile не найдены. Создайте релизный профиль в File > Build Profiles.", MessageType.Warning);
            }
            foreach (var guid in guidsArray)
            {
                var index = IndexOf(listProperty, guid);
                var label = AssetDatabase.GUIDToAssetPath(guid);
                var enabled = EditorGUILayout.ToggleLeft(label, index >= 0);
                if (enabled && index < 0)
                {
                    listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
                    listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1).stringValue = guid;
                }
                else if (!enabled && index >= 0)
                {
                    listProperty.DeleteArrayElementAtIndex(index);
                }
            }
        }

        /// <summary>Returns the index of the GUID in the serialized list or -1.</summary>
        private static int IndexOf(SerializedProperty listProperty, string guid)
        {
            for (var index = 0; index < listProperty.arraySize; index++)
            {
                if (listProperty.GetArrayElementAtIndex(index).stringValue == guid)
                {
                    return index;
                }
            }
            return -1;
        }

        /// <summary>Draws password and fingerprint inputs that write only to the per-user file, never to the project.</summary>
        private void DrawSecrets()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Локальные секреты (не сохраняются в проекте и Git)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_secrets.DescribeSources() + $" Переменные окружения: {RemoteContentSecretStore.PasswordVariableName}, {RemoteContentSecretStore.FingerprintVariableName}.", MessageType.None);
            _passwordInput = EditorGUILayout.PasswordField("Пароль SFTP", _passwordInput);
            if (GUILayout.Button("Сохранить пароль локально") && !string.IsNullOrEmpty(_passwordInput))
            {
                _secrets.SavePassword(_passwordInput);
                _passwordInput = string.Empty;
            }
            _fingerprintInput = EditorGUILayout.TextField("SSH-отпечаток сервера", _fingerprintInput);
            if (GUILayout.Button("Сохранить отпечаток локально") && !string.IsNullOrWhiteSpace(_fingerprintInput))
            {
                _secrets.SaveHostKeyFingerprint(_fingerprintInput);
                _fingerprintInput = string.Empty;
            }
        }

        /// <summary>Draws the connection check button: opens a session and lists the destination, changing nothing on the server.</summary>
        private void DrawConnectionCheck(RemoteContentPublishData data)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Проверить подключение"))
            {
                var result = new RemoteContentConnectionChecker(new WinScpSftpSessionFactory()).Check(SftpConnectionData.Create(data, _secrets));
                var succeeded = result.Status == PublishStatusAlias.Succeeded;
                _statusText = succeeded ? "Подключение успешно: папка назначения доступна для просмотра." : $"Ошибка ({result.Stage}): {result.Message}";
                _statusType = succeeded ? MessageType.Info : MessageType.Error;
            }
            if (!string.IsNullOrEmpty(_statusText))
            {
                EditorGUILayout.HelpBox(_statusText, _statusType);
            }
        }
    }
}
