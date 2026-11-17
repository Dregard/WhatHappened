using System;
using System.IO;
using Game.Profile.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Profile.Serialization
{
    public class DeserializationController
    {
        public ProfileData ReadFromFile()
        {
            var mainProfileFilePath = ProfileHelper.GetMainProfileFilePath();
            if (File.Exists(mainProfileFilePath))
            {
                Debug.Log("Read main profile");

                if (TryReadFile(mainProfileFilePath, out var profileData))
                {
                    return profileData;
                }
            }

            var backupProfileFilePath = ProfileHelper.GetBackupProfileFilePath();
            if (File.Exists(backupProfileFilePath))
            {
                Debug.Log("Read backup profile");

                if (TryReadFile(backupProfileFilePath, out var profileData))
                {
                    return profileData;
                }
            }

            Debug.Log("Profile file does not exist, create a new one");

            return new ProfileData();
        }

        private bool TryReadFile(string profileFilePath, out ProfileData profileData)
        {
            var profileDataJson = File.ReadAllText(profileFilePath);

            if (string.IsNullOrEmpty(profileDataJson))
            {
                profileData = null;
                return false;
            }

            return TryDeserializeProfileData(profileDataJson, out profileData);
        }

        private bool TryDeserializeProfileData(string json, out ProfileData profileData)
        {
            try
            {
                profileData = JsonConvert.DeserializeObject<ProfileData>(json);
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"Deserializing profile error: {e}");
            }

            profileData = null;
            return false;
        }
    }
}