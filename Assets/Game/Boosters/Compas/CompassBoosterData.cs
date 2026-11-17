using UnityEngine;
using Game.Anomalies;

namespace Game.Boosters
{
    [CreateAssetMenu(fileName = "CompassBoosterData", menuName = "Game/Boosters/CompassBooster")]
    public class CompassBoosterData : BoosterData
    {
        [Tooltip("Active gameplay seconds to point at an unfound current-stage anomaly. Finding it ends the hint early; pause and photo suspend expiry.")]
        [Min(0.01f)]
        public float HintDuration = 30f;
        [Tooltip("Finger appearance and pulse animation; a finger sprite is required before a hint can be spent.")]
        public AnomalyHintData Hint = new();
        [Tooltip("Zero-based level index that unlocks the hint; preserves the existing inventory and unlock progression.")]
        public int OpenLevel = 1;
    }
}
