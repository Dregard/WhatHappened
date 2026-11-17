using UnityEngine;

namespace Game.Boosters
{
    [CreateAssetMenu(fileName = "FreezeTimerBoosterData", menuName = "Game/Boosters/FreezeTimerBooster")]
    public class FreezeTimerBoosterData : BoosterData
    {
        // Compatibility for existing authoring/verification consumers; photo duration is a fixed product constraint.
        public float Duration { get { return Game.Anomalies.AnomalyBoosterController.PhotoDuration; } }
        [Tooltip("Zero-based level index that unlocks the photo.")]
        public int OpenLevel = 1;
        [Min(0), Tooltip("Photos available at the start of every level once unlocked. Leftovers do not carry over; photos bought or earned during a level are usable until the level ends. Zero disables the per-level refill and keeps accumulating stock.")]
        public int PhotosPerLevel = 3;
    }
}
