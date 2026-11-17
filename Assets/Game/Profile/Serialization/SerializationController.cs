using System;
using System.IO;
using Game.Profile.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Profile.Serialization
{
    public class SerializationController
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new();
        
        private ProfileData _profileData;
        

        public void SetProfileData(ProfileData profileData)
        {
            _profileData = profileData;
        }

        public void AddJsonConverter(JsonConverter jsonConverter)
        {
            JsonSerializerSettings.Converters.Add(jsonConverter);
        }
        
        public void Save()
        {
            var timeBefore = Time.realtimeSinceStartup;

            if (TrySerializeProfileData(_profileData, out var profileDataJson))
            {
                Debug.Log($"Save profile with version: {_profileData.Version}");

                var timeAfterSerialization = Time.realtimeSinceStartup;
                var serializeTimeMs = (timeAfterSerialization - timeBefore) * 1000;
                var serializeTime = $"{serializeTimeMs:F2} ms";

                WriteToFile(profileDataJson);

                var timeAfterWriting = Time.realtimeSinceStartup;
                var writeTimeMs = (timeAfterWriting - timeAfterSerialization) * 1000;
                var writeTime = $"{writeTimeMs:F2} ms";

                Debug.Log(
                    $"Content length: {profileDataJson.Length}, serialize time: {serializeTime}, write time: {writeTime}");
            }
        }

        public void SaveAs(string path)
        {
            Debug.Log("Profile data must be set first");

            if (TrySerializeProfileData(_profileData, out var profileDataJson))
            {
                File.WriteAllText(path, profileDataJson);
            }
        }

        private bool TrySerializeProfileData(ProfileData profileData, out string json)
        {
            try
            {
                profileData.Version++;
                json = JsonConvert.SerializeObject(profileData, JsonSerializerSettings);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Serializing profile error: {e}");
            }

            json = null;
            return false;
        }

        private void WriteToFile(string profileDataJson)
        {
            var profilesDirectory = ProfileHelper.GetProfileDirectory();

            if (!Directory.Exists(profilesDirectory))
                Directory.CreateDirectory(profilesDirectory);

            var canProfileBackup = true;
            try
            {
                File.WriteAllText(ProfileHelper.GetMainProfileFilePath(), profileDataJson);
            }
            catch (Exception e)
            {
                Debug.Log($"Main profile write exception: {e}");
                canProfileBackup = false;
            }

            if (!canProfileBackup)
                return;

            try
            {
                File.WriteAllText(ProfileHelper.GetBackupProfileFilePath(), profileDataJson);
            }
            catch (Exception e)
            {
                Debug.Log($"Backup profile write exception: {e}");
            }
        }
    }
}