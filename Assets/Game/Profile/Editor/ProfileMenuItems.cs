using System.IO;
using Game.Profile.Utils;
using UnityEditor;
using UnityEngine;

namespace Game.Profile.Editor
{
    public static class ProfileMenuItems
    {
        [MenuItem("Game/Profile/Show")]
        public static void Show()
        {
            var profileDirectory = ProfileHelper.GetProfileDirectory();

            if (!Directory.Exists(profileDirectory))
                Directory.CreateDirectory(profileDirectory);

            EditorUtility.RevealInFinder(profileDirectory);
        }

        [MenuItem("Game/Profile/Delete")]
        public static void Delete()
        {
            var profileDirectory = ProfileHelper.GetProfileDirectory();

            if (Directory.Exists(profileDirectory))
                Directory.Delete(profileDirectory, true);
        }

        [MenuItem("Game/Profile/Apply From ...")]
        private static void ApplyFrom()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("Applying profile from file not work in Play mode");
                return;
            }

            var loadPath = EditorUtility.OpenFilePanel("Select Profile To Apply", "", "json");
            var destinationPath = ProfileHelper.GetMainProfileFilePath();

            FileUtil.ReplaceFile(loadPath, destinationPath);
        }
    }
}