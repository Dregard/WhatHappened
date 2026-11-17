using System;
using System.Collections.Generic;
using Game.Profile;

namespace Game.Boosters
{
    [Serializable]
    public class BoostersProfile : IProfile
    {
        public Dictionary<string, int> BoosterCounts = new();
        public List<BoosterKind> OpenedBoosters = new();
    }
}
