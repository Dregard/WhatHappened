using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Anomalies;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    /// <summary>Plays the all-evidences effect (a glowing beam from under every evidence down to the top rim of the unsolved circle, then a particle burst inside it) and advances when it ends, as before.</summary>
    public sealed class ChapterAllEvidencesStage : ChapterResultStage
    {
        private const int BurstCount = 14;
        private const float StaggerPerBeam = 0.05f;
        private const float BeamTravelShare = 0.6f;

        [SerializeField] private RectTransform _content;
        [SerializeField] private Image _streakTemplate;
        [SerializeField] private Image _particleTemplate;

        private static readonly ChapterSharedLayout StageLayout = new()
        {
            Row = new ChapterRowLayout(328f, 182f, 780f, 0.264f, 1920f),
            Logo = new ChapterAnchors(0.099f, 0.143f, 0.894f, 0.305f),
            Contour = new ChapterAnchors(0.271f, 0.345f, 0.729f, 0.603f),
            Question = new ChapterAnchors(0.333f, 0.385f, 0.667f, 0.563f),
        };

        public override ChapterSharedLayout Layout { get { return StageLayout; } }

        [Header("Convergence effect")]
        [SerializeField, Range(0.1f, 1f), Tooltip("Visible radius of the dashed circle as a fraction of the half-width of the Contour rectangle; the sprite keeps transparent glow around the dashes. Beam ends and the particle burst are sized from it.")]
        private float _rimRadiusShare = 0.856f;
        [SerializeField, Min(1f), Tooltip("Width of one glowing beam, in reference pixels. Larger values make thicker beams.")]
        private float _beamWidth = 60f;
        [SerializeField, Min(0f), Tooltip("Horizontal distance between the rim points the beams arrive at, in reference pixels. 0 makes all beams hit the same top point.")]
        private float _beamSpread = 60f;
        [SerializeField, Min(0f), Tooltip("Gap between the bottom of an evidence circle and the start of its beam, in reference pixels.")]
        private float _beamStartGap = 12f;
        [SerializeField, Range(0.1f, 1f), Tooltip("Particle burst radius as a fraction of the dashed circle radius; 1 reaches the dashes.")]
        private float _burstRadiusShare = 0.95f;

        protected override void Prepare(ChapterResultWindowModel model)
        {
        }

        public override UniTask Play(ChapterResultWindowModel model, CancellationToken token)
        {
            return PlayConvergence(model.ConvergenceDuration, token);
        }

        private async UniTask PlayConvergence(float seconds, CancellationToken token)
        {
            var duration = Mathf.Max(0.01f, seconds);
            var contourRect = AnomalyUi.LocalRect(Shared.Contour, _content);
            var target = contourRect.center;
            var rimRadius = contourRect.width * 0.5f * _rimRadiusShare;
            var streaks = new List<Image>();
            var starts = new List<Vector2>();
            var ends = new List<Vector2>();
            var slots = Shared.Row.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                var streak = Instantiate(_streakTemplate, _content, false);
                streak.rectTransform.anchorMin = streak.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                streak.rectTransform.pivot = new Vector2(0.5f, 0f);
                streaks.Add(streak);
                var slotRect = AnomalyUi.LocalRect(slots[i].Rect, _content);
                starts.Add(new Vector2(slotRect.center.x, slotRect.yMin - _beamStartGap));
                var offsetX = Mathf.Clamp((i - (slots.Count - 1) * 0.5f) * _beamSpread, -rimRadius * 0.9f, rimRadius * 0.9f);
                ends.Add(target + new Vector2(offsetX, Mathf.Sqrt(rimRadius * rimRadius - offsetX * offsetX)));
            }
            var burst = new List<Image>();
            for (var i = 0; i < BurstCount; i++)
                burst.Add(Instantiate(_particleTemplate, _content, false));
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                var burstT = Mathf.Clamp01((t - BeamTravelShare) / (1f - BeamTravelShare));
                for (var i = 0; i < streaks.Count; i++)
                {
                    var local = Mathf.Clamp01((t - StaggerPerBeam * i) / BeamTravelShare);
                    var head = Vector2.Lerp(starts[i], ends[i], Mathf.SmoothStep(0, 1, local));
                    PlaceBeam(streaks[i].rectTransform, starts[i], head);
                    streaks[i].color = new Color(1, 1, 1, local > 0 ? 1f - burstT : 0f);
                }
                for (var i = 0; i < burst.Count; i++)
                {
                    var angle = Mathf.PI * 2 * i / burst.Count;
                    var position = target + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rimRadius * _burstRadiusShare * burstT;
                    AnomalyUi.Place(burst[i].rectTransform, new Rect(position - Vector2.one * 30f, Vector2.one * 60f));
                    burst[i].color = new Color(1, 1, 1, burstT > 0 ? 1 - burstT : 0);
                }
                await UniTask.NextFrame(token);
            }
            foreach (var image in streaks)
                Destroy(image.gameObject);
            foreach (var image in burst)
                Destroy(image.gameObject);
        }

        /// <summary>Stretches a beam from its start to the head: the bright end of the trail sprite sits at the head and the faded tail points back at the start.</summary>
        /// <param name="beam">Beam rect with its pivot at the bottom center and anchors at the content center.</param>
        /// <param name="start">Beam start in content-local coordinates.</param>
        /// <param name="head">Current beam head in content-local coordinates.</param>
        private void PlaceBeam(RectTransform beam, Vector2 start, Vector2 head)
        {
            var back = start - head;
            beam.localPosition = head;
            beam.sizeDelta = new Vector2(_beamWidth, back.magnitude);
            beam.localRotation = back.sqrMagnitude > 0.0001f ? Quaternion.FromToRotation(Vector3.up, back) : Quaternion.identity;
        }
    }
}
