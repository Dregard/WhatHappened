using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Profile
{
    public static class ProfileUtils
    {
        public static void RenameField(this JObject jObject, string currentName, string newName)
        {
            if (jObject.ContainsKey(currentName))
            {
                jObject[newName] = jObject[currentName];
                jObject.Remove(currentName);

                Debug.Log($"Field renamed from {currentName} to {newName}");
            }
        }

        public static bool IsProfileExist(this ProfileData profileData, string profileName)
        {
            return profileData.Profile.ContainsKey(profileName);
        }

        public static void RemoveProfile(this ProfileData profileData, string profileName)
        {
            if (!profileData.Profile.ContainsKey(profileName))
            {
                Debug.Log($"Remove error. No profile with name {profileName}");
                return;
            }

            profileData.Profile.Remove(profileName);
        }

        public static void RenameProfile(this ProfileData profileData, string curProfileName, string newProfileName)
        {
            if (!profileData.Profile.ContainsKey(curProfileName))
            {
                Debug.Log($"Rename error. No profile with name {curProfileName}");
                return;
            }

            profileData.Profile[newProfileName] = profileData.Profile[curProfileName];
            profileData.Profile.Remove(curProfileName);
        }

        public static T GetOrCreateProfile<T>(this ProfileData profileData, string profileName) where T : IProfile
        {
            if (!profileData.Profile.TryGetValue(profileName, out var profileObject))
            {
                Debug.Log($"Profile data does not contain profile {profileName}");
                return default;
            }

            IProfile profile = null;

            switch (profileObject)
            {
                case JObject jObject:
                    TryConvertToProfile(jObject, typeof(T), out var convertedProfile);
                    profile = convertedProfile;
                    break;

                case IProfile trueProfile:
                    profile = trueProfile;
                    break;
            }

            if (profile is T result)
                return result;

            return default;
        }

        public static bool TryConvertToProfile(JObject jObject, Type profileType, out IProfile profile)
        {
            try
            {
                var convertedProfile = jObject.ToObject(profileType, ProfileSystem.JsonSerializer);
                profile = convertedProfile as IProfile;
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"Converting object to profile {profileType} error: {e}");
            }

            profile = null;
            return false;
        }
    }
}