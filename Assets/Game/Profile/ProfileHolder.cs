using System;
using Newtonsoft.Json.Linq;
using Zenject;

namespace Game.Profile
{
    public abstract class ProfileHolder
    {
        protected readonly SignalBus SignalBus;
        public string ProfileName { get; }

        public abstract Type ProfileType { get; }


        protected ProfileHolder(SignalBus signalBus, string profileName)
        {
            SignalBus = signalBus;
            ProfileName = profileName;
            
            signalBus.Fire(new Signals.ProfileHolderAppeared { ProfileHolder = this });
        }

        public abstract void ApplyProfile(IProfile profile);

        public abstract IProfile CreateAndApplyDefaultProfile();

        protected abstract void OnProfileSet();

        public void PrepareProfile(JObject jObject)
        {
            OnPrepareProfile(jObject);
        }

        protected virtual void OnPrepareProfile(JObject jObject)
        {
        }

        public void Start()
        {
            OnStart();
        }

        protected virtual void OnStart()
        {
        }

        public void Reset()
        {
            OnReset();
        }

        protected virtual void OnReset()
        {
        }
    }
}