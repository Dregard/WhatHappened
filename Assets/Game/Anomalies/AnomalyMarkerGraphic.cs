using UnityEngine;
using UnityEngine.UI;

namespace Game.Anomalies
{
    public sealed class AnomalyMarkerGraphic : MaskableGraphic
    {
        private float _progress;
        public float Width { get; set; }
        public float Progress
        {
            get { return _progress; }
            set { _progress = Mathf.Clamp01(value); SetVerticesDirty(); }
        }

        /// <summary>Draws a continuous elliptical marker progressively around the full anomaly rectangle.</summary>
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            const int segments = 80;
            var rect = rectTransform.rect;
            var radius = rect.size * 0.5f;
            var inner = Vector2.Max(Vector2.zero, radius - Vector2.one * Width);
            var count = Mathf.CeilToInt(segments * _progress);
            for (var i = 0; i < count; i++)
            {
                var start = (float)i / segments * Mathf.PI * 2;
                var end = Mathf.Min((float)(i + 1) / segments, _progress) * Mathf.PI * 2;
                var a = new Vector2(Mathf.Cos(start), Mathf.Sin(start));
                var b = new Vector2(Mathf.Cos(end), Mathf.Sin(end));
                var index = helper.currentVertCount;
                helper.AddVert(rect.center + a * radius, color, Vector2.zero);
                helper.AddVert(rect.center + b * radius, color, Vector2.zero);
                helper.AddVert(rect.center + b * inner, color, Vector2.zero);
                helper.AddVert(rect.center + a * inner, color, Vector2.zero);
                helper.AddTriangle(index, index + 1, index + 2);
                helper.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
