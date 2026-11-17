using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Anomalies
{
    [Serializable]
    public sealed class AnomalyLevelData
    {
        [Tooltip("Original picture displayed before Ready, and beneath anomaly images during search.")]
        public Sprite MainImage;
        [Tooltip("Optional original photo shown before Ready, in the photo flight, thumbnail and photo booster. Use it when the original contains objects that vanish for good once the search begins; the board then switches to MainImage. Empty uses MainImage.")]
        public Sprite OriginalImage;
        [Tooltip("Clue awarded by completing the level; available to chapter presentation.")]
        public Sprite ClueImage;
        [Tooltip("Evidence name shown on the evidence-found screen, for example a strange green test tube. Empty hides the caption.")]
        public string ClueName;
        [Tooltip("Narrative text shown on the paper plate before the player taps to begin the investigation. Empty hides the plate.")]
        public string IntroText;
        [Tooltip("Available anomalies. IDs must be unique; not every entry has to appear in an attempt.")]
        public List<AnomalyData> AnomaliesList = new();
        [Tooltip("Search stages in play order. Empty stages still pass through the presentation completion gate.")]
        public List<AnomalyStageData> StagesList = new();
        [Min(0), Tooltip("Seconds to draw a complete marker around a finding. Fading cannot begin before this ends.")]
        public float FindingDuration = 0.4f;
        [Min(0), Tooltip("Seconds for each transition fade: flickering to full black, then revealing the next stage. Search stays locked through both.")]
        public float TransitionDuration = 0.3f;
        [Tooltip("Visual tuning shared by this level's photo, findings, HUD and blackout transitions.")]
        public AnomalyPresentationData Presentation = new();

        /// <summary>Picture representing the original scene: OriginalImage when assigned, otherwise MainImage.</summary>
        public Sprite PhotoImage => OriginalImage != null ? OriginalImage : MainImage;
    }
}
