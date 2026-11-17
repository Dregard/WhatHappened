using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Anomalies
{
    public sealed class AnomalyFindingEffects : MonoBehaviour
    {
        private AnomalySearchSystem _search;
        private AnomalyLevelData _data;
        private RectTransform _root;

        /// <summary>Scopes all animation clocks and spawned graphics to the owning search view.</summary>
        public void Initialize(AnomalySearchSystem search, AnomalyLevelData data)
        {
            _search = search;
            _data = data;
            _root = (RectTransform)transform;
        }

        /// <summary>Animates a paused-aware interval; zero duration still applies the final state.</summary>
        public IEnumerator Animate(float duration, Action<float> frame)
        {
            var elapsed = 0f;
            frame(0);
            while (elapsed < Mathf.Max(0, duration))
            {
                yield return null;
                if (_search.IsPaused)
                    continue;
                elapsed += Time.deltaTime;
                frame(Mathf.Clamp01(elapsed / duration));
            }
            frame(1);
        }

        /// <summary>Draws first, then fades and flies independently; acknowledgement waits for every effect and HUD arrival.</summary>
        public IEnumerator Finding(AnomalyData anomaly, Image image, Image slot, Action completed)
        {
            var tuning = _data.Presentation;
            var markerObject = new GameObject("Marker", typeof(RectTransform), typeof(CanvasRenderer));
            markerObject.layer = image.gameObject.layer;
            markerObject.transform.SetParent(image.transform, false);
            var marker = markerObject.AddComponent<AnomalyMarkerGraphic>();
            AnomalyUi.Bounds(marker.rectTransform, Vector2.zero, Vector2.one);
            marker.raycastTarget = false;
            marker.color = tuning.MarkerColor;
            marker.Width = tuning.MarkerWidth;
            var yep = AnomalyUi.Text("Yep", image.transform, new Vector2(-0.5f, 1f), new Vector2(1.5f, 1.6f));
            UiKit.Style(UiThemeData.LoadOrDefault(), yep, UiKit.TextStyleAlias.Light);
            yep.text = "Yep!";
            yep.color = tuning.MarkerColor;
            var starsArray = new Image[Mathf.Clamp(tuning.StarCount, 0, 24)];
            var starSprite = UiThemeData.LoadOrDefault().Particle;
            for (var i = 0; i < starsArray.Length; i++)
                starsArray[i] = AnomalyUi.Image("FindingStar", image.transform, starSprite);
            var outline = Mathf.Max(0, _data.FindingDuration);
            var fade = Mathf.Max(0, tuning.FadeDuration);
            var flightTime = Mathf.Max(0, tuning.FlightDuration);
            var starsTime = Mathf.Max(0, tuning.StarsDuration);
            var duration = Mathf.Max(outline + Mathf.Max(fade, flightTime), starsTime);
            Image flight = null;
            var arrived = false;
            yield return Animate(duration, progress =>
            {
                var elapsed = progress * duration;
                marker.Progress = Fraction(elapsed, outline);
                for (var i = 0; i < starsArray.Length; i++)
                {
                    var starProgress = Fraction(elapsed, starsTime);
                    var angle = Mathf.PI * 2 * i / starsArray.Length;
                    var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * tuning.StarDistance * starProgress;
                    AnomalyUi.Place(starsArray[i].rectTransform, new Rect(offset - Vector2.one * tuning.StarSize * 0.5f,
                        Vector2.one * tuning.StarSize));
                    starsArray[i].color = Color.Lerp(tuning.StarColor, Color.clear, starProgress);
                }
                if (elapsed < outline)
                    return;
                if (flight == null)
                    flight = AnomalyUi.Image("FoundFlight", transform, anomaly.Image);
                var afterOutline = elapsed - outline;
                image.color = new Color(1, 1, 1, 1 - Fraction(afterOutline, fade));
                marker.canvasRenderer.SetAlpha(1 - Fraction(afterOutline, fade));
                yep.alpha = 1 - Fraction(afterOutline, fade);
                var fraction = Mathf.SmoothStep(0, 1, Fraction(afterOutline, flightTime));
                var source = FitRect(AnomalyUi.LocalRect(image.rectTransform, _root), anomaly.Image);
                var target = FitRect(AnomalyUi.LocalRect(slot.rectTransform, _root), anomaly.Image);
                flight.preserveAspect = true;
                AnomalyUi.Place(flight.rectTransform, new Rect(Vector2.Lerp(source.center, target.center, fraction) -
                    Vector2.Lerp(source.size, target.size, fraction) * 0.5f, Vector2.Lerp(source.size, target.size, fraction)));
                if (!arrived && fraction >= 1)
                {
                    arrived = true;
                    UiKit.MarkFound(slot, anomaly.Image);
                    flight.gameObject.SetActive(false);
                }
            });
            image.gameObject.SetActive(false);
            if (flight != null)
                Destroy(flight.gameObject);
            completed();
        }

        /// <summary>Fits the sprite aspect inside an area, centered, without stretching either axis.</summary>
        private static Rect FitRect(Rect area, Sprite sprite)
        {
            if (sprite == null || sprite.rect.width <= 0 || sprite.rect.height <= 0 || area.width <= 0 || area.height <= 0)
                return area;
            var scale = Mathf.Min(area.width / sprite.rect.width, area.height / sprite.rect.height);
            var size = new Vector2(sprite.rect.width, sprite.rect.height) * scale;
            return new Rect(area.center - size * 0.5f, size);
        }

        /// <summary>Shows a nonblocking miss at the exact pointer position within the board.</summary>
        public void Miss(RectTransform board, Vector2 local)
        {
            StartCoroutine(MissRoutine(board, local));
        }

        /// <summary>Fades a cross with the same pause and lifetime semantics as finding effects.</summary>
        private IEnumerator MissRoutine(RectTransform board, Vector2 local)
        {
            var tuning = _data.Presentation;
            var cross = AnomalyUi.Text("Miss", board, Vector2.zero, Vector2.one);
            UiKit.Style(UiThemeData.LoadOrDefault(), cross, UiKit.TextStyleAlias.Light);
            cross.text = "×";
            AnomalyUi.Place(cross.rectTransform, new Rect(local - Vector2.one * tuning.MissSize * 0.5f,
                Vector2.one * tuning.MissSize));
            yield return Animate(tuning.MissDuration, progress => cross.color = Color.Lerp(tuning.MissColor, Color.clear, progress));
            Destroy(cross.gameObject);
        }

        /// <summary>Clamps phase progress, including immediate zero-duration phases.</summary>
        private static float Fraction(float elapsed, float duration)
        {
            return duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration);
        }

        /// <summary>Stops all owned animation continuations immediately when the attempt leaves the hierarchy.</summary>
        private void OnDisable()
        {
            StopAllCoroutines();
        }
    }
}
