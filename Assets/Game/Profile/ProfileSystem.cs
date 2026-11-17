using System;
using System.Collections.Generic;
using Game.Profile.Converters;
using Game.Profile.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Zenject;

namespace Game.Profile
{
    public class ProfileSystem
    {
        public const string ProfilesDirectory = "profile";
        public const string MainProfileFileName = "profile.json";
        public const string BackupProfileFileName = "backup.json";

        private readonly Dictionary<Type, ProfileHolder> _profileHoldersByType = new();
        public static readonly JsonSerializer JsonSerializer = new();

        private readonly SerializationController _serializationController = new();
        private readonly DeserializationController _deserializationController = new();

        private ProfileData _profileData;

        private bool _skipFrame;
        private bool _isStarted;

        private bool _isUpdating;


        public ProfileSystem(ProfileHolder[] profileHolders, SignalBus signalBus)
        {
            Initialize();
            
            foreach (var profileHolder in profileHolders)
            {
                RegisterProfileHolder(profileHolder);
            }
            
            signalBus.Subscribe<Signals.ProfileHolderAppeared>(HandleProfileHolderAppeared);
        }

        public void Initialize()
        {
            var converters = JsonConvertersDistributor.GetAllConverters();
            if (converters != null)
            {
                foreach (var jsonConverter in converters)
                {
                    _serializationController.AddJsonConverter(jsonConverter);
                    JsonSerializer.Converters.Add(jsonConverter);
                }
            }

            var profileData = _deserializationController.ReadFromFile();
            SetProfileData(profileData);
        }

        public bool TryGetProfileHolder<THolder>(out THolder tHolder) 
            where THolder : ProfileHolder
        {
            var holderType = typeof(THolder);
            if (_profileHoldersByType.TryGetValue(holderType, out var holder) == false)
            {
                tHolder = null;
                return false;
            }

            tHolder = (THolder) holder;
            return true;
        }

        private void SetProfileData(ProfileData profileData)
        {
            _profileData = profileData;
            
            _serializationController.SetProfileData(profileData);

            foreach (var entry in _profileHoldersByType)
                ApplyProfileToHolder(entry.Value);

            if (_isStarted)
                StartProfileHolders();

            Debug.Log($"Set profile version: {_profileData.Version}");
        }

        public void StartProfileHolders()
        {
            if (_profileData == null)
            {
                Debug.Log("Profile data must be set");
                return;
            }

            _isStarted = true;

            foreach (var entry in _profileHoldersByType)
                entry.Value.Start();
        }

        public void ResetProfileHolders()
        {
            _isStarted = false;

            foreach (var entry in _profileHoldersByType)
                entry.Value.Reset();
        }

        public void RegisterProfileHolder(ProfileHolder profileHolder)
        {
            _profileHoldersByType.Add(profileHolder.GetType(), profileHolder);

            if (_profileData == null)
                return;

            ApplyProfileToHolder(profileHolder);

            if (_isStarted)
                profileHolder.Start();
        }

        private void ApplyProfileToHolder(ProfileHolder profileHolder)
        {
            _profileData.Profile.TryGetValue(profileHolder.ProfileName, out var profileObject);

            IProfile profile = null;

            switch (profileObject)
            {
                case JObject jObject:
                {
                    profileHolder.PrepareProfile(jObject);

                    if (ProfileUtils.TryConvertToProfile(jObject, profileHolder.ProfileType, out var convertedProfile))
                        _profileData.Profile[profileHolder.ProfileName] = convertedProfile;

                    profile = convertedProfile;
                    break;
                }
                case IProfile trueProfile:
                {
                    profile = trueProfile;
                    break;
                }
            }

            if (profile != null)
            {
                profileHolder.ApplyProfile(profile);
            }
            else
            {
                profile = profileHolder.CreateAndApplyDefaultProfile();
                _profileData.Profile[profileHolder.ProfileName] = profile;
            }
        }

        public void SaveAs(string savePath)
        {
            _serializationController.SaveAs(savePath);
        }

        public void Save()
        {
            _serializationController.Save();
        }

        private void HandleProfileHolderAppeared(Signals.ProfileHolderAppeared signal)
        {
            RegisterProfileHolder(signal.ProfileHolder);
        }
    }
}