using System.IO;
using UnityEngine;

namespace Game.Profile.Utils
{
    public static class ProfileHelper
    {
        public static string GetProfileDirectory()
        {
            return Path.Combine(Application.persistentDataPath, ProfileSystem.ProfilesDirectory);
        }

        public static string GetMainProfileFilePath()
        {
            return Path.Combine(GetProfileDirectory(), ProfileSystem.MainProfileFileName);
        }

        public static string GetBackupProfileFilePath()
        {
            return Path.Combine(GetProfileDirectory(), ProfileSystem.BackupProfileFileName);
        }
    }
}