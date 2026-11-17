using UnityEngine;

namespace Game.Levels
{
    public class LevelData : ScriptableObject
    {
        [Tooltip("Anomaly-search content. MainImage is required; existing Addressables keys and level time are preserved.")]
        public Game.Anomalies.AnomalyLevelData SearchData = new();
        public int LevelTime;
    }
}
