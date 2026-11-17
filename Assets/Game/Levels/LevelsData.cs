using System.Collections.Generic;
using Game.Purchase;
using Game.Tutorial;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(fileName = "LevelsData", menuName = "Game/Levels/LevelsData")]
    public class LevelsData : ScriptableObject
    {
        [Tooltip("Ordered chapters, repeated from the first after the last. Empty retains legacy level progression. Each chapter requires levels and each search level requires a clue image.")]
        public ChapterData[] ChaptersArray = System.Array.Empty<ChapterData>();
        [Min(0f), Tooltip("Seconds the evidence travels from the evidence-found screen into its slot in the evidence row.")]
        public float EvidenceFlightDuration = 0.6f;
        [Min(0f), Tooltip("Seconds the all-evidences screen plays its effect, with evidence streaks converging on the unsolved culprit, before the culprit is revealed.")]
        public float AllEvidenceDuration = 2.2f;
        [Min(0f), Tooltip("Seconds the culprit reveal stays before moving on; a tap skips the wait. Zero requires a tap.")]
        public float CulpritRevealDuration = 2.5f;
        [Min(0f), Tooltip("Seconds of the sunburst rotation per full turn behind celebrated items. Zero keeps it still.")]
        public float SunburstTurnSeconds = 18f;
        public PriceData RevivePrice;
        public float ReviveTimer;
        public LevelReference[] Levels;
        public List<TutorialData> Tutorials;
        [HideInInspector, Tooltip("Legacy first loop level, kept only to migrate existing authored assets.")]
        public int LoopStartLevel;

        [SerializeField, Min(1), Tooltip("Number of downloaded levels to keep ready, including the current level. Minimum 1; limited by available distinct levels.")]
        private int _downloadReserveCount = 3;

        [SerializeField, Min(1), Tooltip("Number of final levels retained on disk and repeated after the main sequence. Minimum 1; values above the catalog size retain all levels.")]
        private int _loopLevelCount = 1;

        [SerializeField, Tooltip("Shuffle the retained final levels once and repeat that saved order. When disabled, repeat them in their original catalog order.")]
        private bool _randomizeLoopOrder = true;

        [SerializeField, Min(1f), Tooltip("Real seconds between background attempts to refill the download reserve after a failure, independent of game time scale. Does not delay the player's Retry action.")]
        private float _downloadRetryInterval = 15f;

        public int DownloadReserveCount
        {
            get { return Mathf.Max(1, _downloadReserveCount); }
        }

        public int LoopLevelCount
        {
            get { return Mathf.Clamp(_loopLevelCount, 1, Mathf.Max(1, Levels.Length)); }
        }

        public bool RandomizeLoopOrder
        {
            get { return _randomizeLoopOrder; }
        }

        public float DownloadRetryInterval
        {
            get { return Mathf.Max(1f, _downloadRetryInterval); }
        }
    }

    [System.Serializable]
    public struct TutorialData
    {
        public int LevelNumber;
        public TutorialController TutorialPrefab;
    }
}
