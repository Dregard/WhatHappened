using System.Collections.Generic;

namespace Game.Profile
{
    public class ProfileData
    {
        public readonly Dictionary<string, object> Profile = new();
        
        public long Version;
    }
}