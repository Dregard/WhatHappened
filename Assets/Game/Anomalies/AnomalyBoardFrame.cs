using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Anomalies
{
    /// <summary>Sits on the board's free area and keeps the decorative frame tightly around the picture that the aspect fitter actually shows, instead of around the whole free area.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class AnomalyBoardFrame : UIBehaviour
    {
        [SerializeField] private RectTransform _frame;
        [SerializeField] private AspectRatioFitter _pictureFitter;
        [Tooltip("Frame border outside the picture in parent units: left, bottom, right, top.")]
        [SerializeField] private Vector4 _padding = new Vector4(30f, 39f, 32f, 24f);

        /// <summary>Recomputes the frame so it surrounds the picture rectangle that the fitter produces inside this free area.</summary>
        public void Fit()
        {
            var area = (RectTransform)transform;
            var parent = (RectTransform)_frame.parent;
            var free = area.rect.size;
            var ratio = _pictureFitter.aspectRatio;
            if (free.x <= 0f || free.y <= 0f || ratio <= 0f)
                return;
            var picture = free.x / free.y > ratio ? new Vector2(free.y * ratio, free.y) : new Vector2(free.x, free.x / ratio);
            var min = area.rect.center - picture * 0.5f - new Vector2(_padding.x, _padding.y);
            var max = area.rect.center + picture * 0.5f + new Vector2(_padding.z, _padding.w);
            var parentMin = parent.InverseTransformPoint(area.TransformPoint(min));
            var parentMax = parent.InverseTransformPoint(area.TransformPoint(max));
            var size = new Vector2(parentMax.x - parentMin.x, parentMax.y - parentMin.y);
            var center = new Vector2(parentMin.x, parentMin.y) + size * 0.5f;
            _frame.anchorMin = new Vector2(0.5f, 0.5f);
            _frame.anchorMax = new Vector2(0.5f, 0.5f);
            _frame.pivot = new Vector2(0.5f, 0.5f);
            _frame.sizeDelta = size;
            _frame.anchoredPosition = center - parent.rect.center;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (_frame != null && _pictureFitter != null)
                Fit();
        }
    }
}
