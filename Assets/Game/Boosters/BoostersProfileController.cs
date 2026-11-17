using Game.Profile;
using Zenject;

namespace Game.Boosters
{
    public class BoostersProfileController : ProfileHolderBase<BoostersProfile>
    {
        public BoostersProfileController(SignalBus signalBus) : base(signalBus, "Boosters")
        {
        }

        protected override BoostersProfile CreateDefaultProfile()
        {
            return new BoostersProfile();
        }

        /// <summary>Migrates the old inventory key, preserving the numeric unlock ID.</summary>
        protected override void OnProfileSet()
        {
            if (Profile.BoosterCounts.TryGetValue("Refresh", out var count))
            {
                var id = BoosterKind.MagicWand.ToString();
                Profile.BoosterCounts.TryGetValue(id, out var existingCount);
                Profile.BoosterCounts[id] = existingCount + count;
                Profile.BoosterCounts.Remove("Refresh");
            }
        }

        public int GetBoosterCount(BoosterKind kind)
        {
            var id = kind.ToString();
            if (Profile.BoosterCounts.TryGetValue(id, out var count))
            {
                return count;
            }
            return 0;
        }

        public void AddBooster(BoosterKind kind, int count)
        {
            var id = kind.ToString();
            if (Profile.BoosterCounts.ContainsKey(id))
            {
                Profile.BoosterCounts[id] += count;
            }
            else
            {
                Profile.BoosterCounts[id] = count;
            }
        }

        /// <summary>Replaces the stored stock, used by per-level refills that must not accumulate.</summary>
        public void SetBoosterCount(BoosterKind kind, int count)
        {
            Profile.BoosterCounts[kind.ToString()] = UnityEngine.Mathf.Max(0, count);
        }

        public bool UseBooster(BoosterKind kind)
        {
            var id = kind.ToString();
            if (Profile.BoosterCounts.TryGetValue(id, out var count) && count > 0)
            {
                Profile.BoosterCounts[id]--;
                return true;
            }
            return false;
        }

        public bool IsOpened(BoosterKind kind)
        {
            return Profile.OpenedBoosters.Contains(kind);
        }

        public void OpenBooster(BoosterKind kind)
        {
            if (!Profile.OpenedBoosters.Contains(kind))
            {
                Profile.OpenedBoosters.Add(kind);
            }
        }
    }
}
