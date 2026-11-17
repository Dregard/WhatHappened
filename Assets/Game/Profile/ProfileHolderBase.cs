using System;
using UnityEngine;
using Zenject;

namespace Game.Profile
{
    public abstract class ProfileHolderBase<TProfile> : ProfileHolder 
        where TProfile : IProfile, new()
    {
        public override Type ProfileType => typeof(TProfile);

        protected TProfile Profile { get; private set; }
        

        protected ProfileHolderBase(SignalBus signalBus, string profileName) : base(signalBus, profileName)
        {
        }

        protected abstract TProfile CreateDefaultProfile();

        public override IProfile CreateAndApplyDefaultProfile()
        {
            var profile = CreateDefaultProfile();
            ApplyProfile(profile);

            return profile;
        }

        public override void ApplyProfile(IProfile profile)
        {
            if (profile is TProfile trueProfile)
            {
                Profile = trueProfile;
                OnProfileSet();
            }
            else
            {
                Debug.Log($"The profile but must be {ProfileType} but got {profile.GetType()}");
            }
        }
    }
}