using System;
using UnityEngine;

namespace Game.Anomalies
{
    [Serializable]
    public sealed class AnomalyPresentationData
    {
        [Min(0), Tooltip("Seconds for the shutter flash and original-image travel to the thumbnail. Search starts afterward.")]
        public float PhotoDuration = 0.65f;
        [Min(0), Tooltip("Seconds to fade a found layer after its marker is complete.")]
        public float FadeDuration = 0.3f;
        [Min(0), Tooltip("Seconds for the found image to reach its HUD slot. The slot opens only on arrival.")]
        public float FlightDuration = 0.6f;
        [Min(0), Tooltip("Seconds for star particles to spread and fade. Stage completion waits for them.")]
        public float StarsDuration = 0.5f;
        [Min(0), Tooltip("Seconds for a miss cross to fade beneath the pointer.")]
        public float MissDuration = 0.4f;
        [Range(0, 8), Tooltip("Number of dim light pulses before full blackout. Stage replacement happens only at full black.")]
        public int FlickerCount = 2;
        [Min(0), Tooltip("Seconds to hold full black around the stage replacement.")]
        public float BlackoutDuration = 0.12f;
        [Tooltip("Color of the progressively drawn finding marker.")]
        public Color MarkerColor = new(0.3f, 1f, 0.5f, 1f);
        [Min(1), Tooltip("Marker stroke width in local UI units; scales with the field.")]
        public float MarkerWidth = 5f;
        [Range(0, 24), Tooltip("Number of stars emitted for each accepted finding.")]
        public int StarCount = 7;
        [Min(0), Tooltip("Star travel radius in local UI units.")]
        public float StarDistance = 100f;
        [Min(1), Tooltip("Star diameter in local UI units.")]
        public float StarSize = 28f;
        [Tooltip("Color of finding stars.")]
        public Color StarColor = new(1f, 0.8f, 0.2f, 1f);
        [Min(1), Tooltip("Miss cross diameter in local UI units.")]
        public float MissSize = 45f;
        [Tooltip("Miss cross color; alpha fades to zero.")]
        public Color MissColor = Color.red;
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
