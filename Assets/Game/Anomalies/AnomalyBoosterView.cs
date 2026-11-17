using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Anomalies
{
    public sealed class AnomalyBoosterView : MonoBehaviour
    {
        private AnomalyBoosterController _controller;
        private AnomalySearchSystem _search;
        private Image _ring;
        private Image _finger;
        private Image _photo;
        private TMP_Text _countdown;
        private TMP_Text _backText;
        private float _pulseTime;

        /// <summary>Creates a passive board-relative finger and a fullscreen raycast-blocking polaroid of the original above the field.</summary>
        public void Initialize(AnomalyBoosterController controller, AnomalySearchSystem search, RectTransform picture, Sprite original)
        {
            _controller = controller;
            _search = search;
            var theme = UiThemeData.LoadOrDefault();
            _ring = AnomalyUi.Image("HintRing", picture, theme.HintTargetRing);
            _ring.preserveAspect = true;
            _finger = AnomalyUi.Image("HintFinger", picture);
            _photo = AnomalyUi.Image("BoosterPhoto", transform);
            _photo.color = theme.PhotoVeilColor;
            _photo.raycastTarget = true;
            var card = UiKit.Image("PolaroidCard", _photo.transform, theme.Polaroid, true);
            UiKit.PlacePadded(card.rectTransform, 64, 373, 1022, 1477, new Vector4(20f, 12f, 20f, 25f));
            var backing = UiKit.Image("PhotoBacking", _photo.transform, null);
            backing.enabled = true;
            backing.color = new Color(0.1f, 0.07f, 0.14f, 1f);
            UiKit.Place(backing.rectTransform, 95, 410, 990, 1385);
            var area = UiKit.Group("PhotoArea", _photo.transform, 95, 410, 990, 1385);
            var image = AnomalyUi.Image("Original", area, original);
            AnomalyUi.Fit(image);
            var pin = UiKit.Image("Pin", _photo.transform, theme.PinRed);
            UiKit.Place(pin.rectTransform, 500, 335, 580, 415);
            var tape = UiKit.Image("OriginalTape", _photo.transform, theme.TapeRed);
            UiKit.Place(tape.rectTransform, 400, 1395, 680, 1490);
            UiKit.FillLabel(theme, tape.transform, "ORIGINAL", UiKit.TextStyleAlias.Light, 56);
            var ring = UiKit.Image("CountdownRing", _photo.transform, theme.TimerRing);
            UiKit.Place(ring.rectTransform, 455, 115, 625, 285);
            _countdown = UiKit.FillLabel(theme, ring.transform, string.Empty, UiKit.TextStyleAlias.Gold, 120);
            UiKit.Label(theme, "Remember", _photo.transform, "Remember the details!", UiKit.TextStyleAlias.Gold, 90, 1610, 990, 1690, 64);
            _backText = UiKit.Label(theme, "BackIn", _photo.transform, string.Empty, UiKit.TextStyleAlias.Light, 190, 1695, 890, 1745, 36);
            _controller.Changed += Refresh;
            Refresh();
        }

        /// <summary>Drives the attempt-owned clocks and animates the finger without per-frame searches or allocations.</summary>
        private void Update()
        {
            _controller.Tick(Time.deltaTime);
            if (_controller.IsPhotoOpen)
            {
                var seconds = Mathf.CeilToInt(_controller.PhotoRemaining);
                _countdown.text = seconds.ToString();
                _backText.text = $"Back to the scene in {seconds} sec";
            }
            _finger.enabled = _controller.HintTarget != null && !_controller.IsPhotoOpen && !_search.IsPaused;
            _ring.enabled = _finger.enabled && _ring.sprite != null;
            if (!_finger.enabled)
                return;
            var data = _controller.HintData;
            _pulseTime += Time.deltaTime;
            var scale = 1 - Mathf.PingPong(_pulseTime / data.PulseDuration * 2, 1) * data.PulseScale;
            _finger.rectTransform.localScale = Vector3.one * scale;
            _ring.rectTransform.localScale = Vector3.one * (2 - scale);
        }

        /// <summary>Maps supplied state to visuals; the sprite fingertip stays on the target at any board scale or aspect ratio.</summary>
        private void Refresh()
        {
            _photo.gameObject.SetActive(_controller.IsPhotoOpen);
            _finger.enabled = _controller.HintTarget != null && !_controller.IsPhotoOpen && !_search.IsPaused;
            _ring.enabled = _finger.enabled && _ring.sprite != null;
            if (_controller.HintTarget == null)
                return;
            var ringRect = _ring.rectTransform;
            ringRect.anchorMin = ringRect.anchorMax = _controller.HintTarget.Bounds.center;
            ringRect.sizeDelta = Vector2.one * 150f;
            ringRect.anchoredPosition = Vector2.zero;
            var data = _controller.HintData;
            var rect = _finger.rectTransform;
            rect.anchorMin = rect.anchorMax = _controller.HintTarget.Bounds.center;
            rect.pivot = data.Fingertip;
            rect.sizeDelta = data.FingerSize;
            rect.anchoredPosition = Vector2.zero;
            _finger.sprite = data.FingerSprite;
            _pulseTime = 0;
            _finger.transform.SetAsLastSibling();
        }

        /// <summary>Detaches presentation and synchronously ends the old attempt's effects when its hierarchy is disabled.</summary>
        private void OnDisable()
        {
            if (_controller == null)
                return;
            _controller.Dispose();
            _controller.Changed -= Refresh;
        }
    }
}
