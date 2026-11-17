using System;
using UnityEngine;

namespace Game.Anomalies
{
    [Serializable]
    public sealed class AnomalyHintData
    {
        [Tooltip("Passive UI finger sprite; its fingertip is positioned at the target rectangle center.")]
        public Sprite FingerSprite;
        [Tooltip("Finger width and height in canvas units; both must be positive.")]
        public Vector2 FingerSize = new Vector2(100, 130);
        [Tooltip("Normalized location of the fingertip within the sprite, measured from its bottom-left corner.")]
        public Vector2 Fingertip = new Vector2(0.3f, 0.95f);
        [Min(0.01f), Tooltip("Active gameplay seconds per repeating finger pulse; pauses stop the animation.")]
        public float PulseDuration = 0.8f;
        [Range(0, 0.5f), Tooltip("Fractional scale reduction at the pressed part of each pulse; zero keeps the finger still.")]
        public float PulseScale = 0.15f;
    }
}
