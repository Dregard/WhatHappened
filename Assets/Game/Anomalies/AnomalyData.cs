using System;
using UnityEngine;

namespace Game.Anomalies
{
    [Serializable]
    public sealed class AnomalyData
    {
        [Tooltip("Unique, case-sensitive ID within this level. Stages refer to this value.")]
        public string Id;
        [Tooltip("Image overlaid on the original picture during search.")]
        public Sprite Image;
        [Tooltip("Rectangle in normalized original-image coordinates, origin bottom-left. Must stay inside 0..1; width and height must be positive.")]
        public Rect Bounds;
        [Tooltip("Description displayed in the search HUD.")]
        public string HudText;
    }
}
