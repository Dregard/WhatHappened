using System;
using UnityEngine;

namespace Game.Anomalies
{
    [Serializable]
    public sealed class AnomalyPresentationData
    {
        [Min(0), Tooltip("Seconds for the shutter flash and original-image travel to the thumbnail. Search starts afterward.")]
        public float PhotoDuration = 0.65f;
        [Min(0), Tooltip("Seconds for the booster photo to fly between the thumbnail and the center, in both directions.")]
        public float BoosterPhotoFlightDuration = 0.5f;
        [Min(0), Tooltip("Seconds for the booster photo captions to fade in after arrival or out when the return starts.")]
        public float BoosterPhotoLabelsFadeDuration = 0.25f;
        [Min(0), Tooltip("Seconds to fade a found layer after its marker is complete.")]
        public float FadeDuration = 0.3f;
        [Min(0), Tooltip("Seconds for the found image to reach its HUD slot. The slot opens only on arrival.")]
        public float FlightDuration = 0.6f;
        [Min(0), Tooltip("Seconds for star particles to spread and fade. Stage completion waits for them.")]
        public float StarsDuration = 0.5f;
        [Range(0, 8), Tooltip("Number of dim light pulses before full blackout. Stage replacement happens only at full black.")]
        public int FlickerCount = 2;
        [Min(0), Tooltip("Seconds to hold full black around the stage replacement.")]
        public float BlackoutDuration = 0.12f;
        [Tooltip("Color of the progressively drawn finding marker.")]
        public Color MarkerColor = new(0.3f, 1f, 0.5f, 1f);
        [Min(1), Tooltip("Base brush width in local UI units; the stroke varies from about 0.6x to 3x of it and scales with the field.")]
        public float MarkerWidth = 5f;
        [Range(0, 24), Tooltip("Number of stars emitted for each accepted finding.")]
        public int StarCount = 7;
        [Min(0), Tooltip("Star travel radius in local UI units.")]
        public float StarDistance = 100f;
        [Min(1), Tooltip("Star diameter in local UI units.")]
        public float StarSize = 28f;
        [Tooltip("Color of finding stars.")]
        public Color StarColor = new(1f, 0.8f, 0.2f, 1f);
        [Min(1), Tooltip("Picture misses within the sliding window that start the tap lock.")]
        public int MissLockCount = 3;
        [Min(0.1f), Tooltip("Sliding window in real seconds in which the misses are counted.")]
        public float MissLockWindow = 1f;
        [Min(0.1f), Tooltip("Real seconds picture taps stay disabled; the level timer keeps running.")]
        public float MissLockDuration = 2f;
        [Range(0, 1), Tooltip("Alpha of the black dimming over the picture while locked.")]
        public float MissLockDim = 0.35f;
        [Tooltip("Warning shown over the picture while locked.")]
        public string MissLockWarning = "Don't tap fast and at random!";
        [Tooltip("Closed HUD slot color. A question mark covers each unfound image.")]
        public Color ClosedColor = new(0.08f, 0.1f, 0.16f, 1f);
        [Min(0), Tooltip("Active seconds a finding's narrative text stays on the paper plate before the task plate returns.")]
        public float NarrativeDuration = 3f;
        [Tooltip("Existing AudioSystem SFX ID for an accepted finding. Empty disables sound.")]
        public string FindingSfx = "GlassSnap";
        [Tooltip("Existing AudioSystem SFX ID for photo capture. Empty disables sound.")]
        public string PhotoSfx = "ButtonClick";
    }
}
