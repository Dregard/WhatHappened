using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.Build.Profile;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Editor composition root for the build callbacks. Unity instantiates processors itself, so the tracker is a single
    /// static instance that only the processors in this folder use.
    /// </summary>
    internal static class RemoteContentPublishHooks
    {
        private static readonly PublishRunTracker Tracker = new PublishRunTracker();

        /// <summary>Subscribes to Addressables build completion once per domain reload.</summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildScript.buildCompleted -= OnAddressablesBuilt;
            BuildScript.buildCompleted += OnAddressablesBuilt;
        }

        /// <summary>Opens a tracked run when a player build starts, before Addressables builds as part of it.</summary>
        internal static void BeginRun(BuildTarget target, string outputPath)
        {
            var profileGuid = GetActiveProfileGuid();
            Tracker.Begin(new PublishRunData(target, outputPath, profileGuid, EditorUserBuildSettings.exportAsGoogleAndroidProject, DateTime.UtcNow));
            var enabled = RemoteContentPublishProjectData.instance.Data.IsProfileEnabled(profileGuid);
            UnityEngine.Debug.Log($"[RemoteContentPublish] Сборка начата: платформа {target}, Build Profile GUID '{(string.IsNullOrEmpty(profileGuid) ? "нет (настройки платформы по умолчанию)" : profileGuid)}', автопубликация для него {(enabled ? "ВКЛЮЧЕНА" : "выключена")}.");
        }

        /// <summary>Closes the tracked run and publishes when the gate allows it.</summary>
        internal static void FinishRun(UnityEditor.Build.Reporting.BuildReport report)
        {
            var run = Tracker.Take();
            var build = PublishBuildSnapshot.FromReport(report, GetActiveProfileGuid(), run);
            var showProgress = !UnityEngine.Application.isBatchMode;
            try
            {
                if (showProgress)
                {
                    EditorUtility.DisplayProgressBar("Remote content", "Publishing Addressables over SFTP...", 0.5f);
                }
                CreateSystem().OnPlayerBuilt(run, build);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"[RemoteContentPublish] APK/AAB создан ({build.OutputPath}), НО публикация удалённого контента НЕ завершена: {exception.Message}");
            }
            finally
            {
                if (showProgress)
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        /// <summary>Returns the GUID of the active Build Profile, or an empty string when the platform's default settings are used.</summary>
        internal static string GetActiveProfileGuid()
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            return profile == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile));
        }

        /// <summary>Creates the production publication system over the real project settings, WinSCP and HTTPS client.</summary>
        internal static RemoteContentPublishSystem CreateSystem()
        {
            var data = RemoteContentPublishProjectData.instance.Data;
            var verifier = new RemoteHashVerifier(new HttpRemoteHashDownloader(), seconds => Thread.Sleep(TimeSpan.FromSeconds(seconds)));
            var publisher = new RemoteContentPublisher(new WinScpSftpSessionFactory(), verifier, null);
            return new RemoteContentPublishSystem(data, new RemoteContentSecretStore(), publisher,
                result => AddressablesOutputResolver.Resolve(AddressableAssetSettingsDefaultObject.Settings, result), File.Exists, new UnityPublishLog());
        }

        /// <summary>Attaches Addressables player build results to the open run; Play Mode and other results are ignored.</summary>
        private static void OnAddressablesBuilt(AddressableAssetBuildResult result)
        {
            if (result is AddressablesPlayerBuildResult playerResult)
            {
                Tracker.RegisterAddressablesResult(playerResult);
            }
        }
    }
}
