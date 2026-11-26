using UnityEngine;
using UnityEngine.UI;

namespace Game.Anomalies
{
    public sealed class AnomalyMarkerGraphic : MaskableGraphic
    {
        private const int Points = 160;
        private const float TurnLength = 1.07f;
        private const float HeadTaperLength = 0.05f;

        private float _progress;
        private float _seed;
        private float _startAngle;
        public float Width { get; set; }
        /// <summary>0 draws an even stroke; 1 gives the full thickness swing.</summary>
        public float ThicknessVariation { get; set; } = 0.4f;
        /// <summary>Outline size relative to the anomaly rectangle.</summary>
        public float SizeScale { get; set; } = 1.2f;
        /// <summary>0 draws a smooth stroke; 1 adds the full path wobble and edge jitter.</summary>
        public float Roughness { get; set; } = 0.3f;
        public float Progress
        {
            get { return _progress; }
            set { _progress = Mathf.Clamp01(value); SetVerticesDirty(); }
        }

        /// <summary>Gives every marker its own hand-drawn character.</summary>
        protected override void OnEnable()
        {
            _seed = Random.Range(0f, 500f);
            _startAngle = Random.Range(0f, Mathf.PI * 2);
            base.OnEnable();
        }

        /// <summary>Draws an uneven brush stroke that follows the anomaly ellipse, growing with the brush head up to Progress.</summary>
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (_progress <= 0)
                return;
            var rect = rectTransform.rect;
            var baseWidth = Mathf.Max(1f, Width);
            var radius = Vector2.Max(Vector2.one, rect.size * 0.5f * Mathf.Max(1f, SizeScale));
            var variation = Mathf.Clamp01(ThicknessVariation);
            var roughness = Mathf.Clamp01(Roughness);
            for (var i = 0; i <= Points; i++)
            {
                var u = (float)i / Points * _progress;
                var angle = _startAngle + u * Mathf.PI * 2 * TurnLength;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var normal = new Vector2(direction.x / radius.x, direction.y / radius.y).normalized;
                var wobble = (Mathf.PerlinNoise(_seed + u * 3f, 1.7f) - 0.5f) * 2f * baseWidth * 1.5f * roughness;
                var pressure = Mathf.SmoothStep(0f, 1f, Mathf.PerlinNoise(_seed + u * 4f, 8.3f));
                var tail = Mathf.SmoothStep(0.2f, 1f, u / HeadTaperLength);
                var head = Mathf.SmoothStep(0.2f, 1f, (_progress - u) / HeadTaperLength);
                var thickness = baseWidth * Mathf.Lerp(1f, 0.6f + pressure * 2.4f, variation) * tail * head;
                var outerJitter = (Mathf.PerlinNoise(_seed + u * 20f, 3.1f) - 0.5f) * baseWidth * 0.9f * roughness;
                var innerJitter = (Mathf.PerlinNoise(_seed + u * 20f, 5.9f) - 0.5f) * baseWidth * 0.9f * roughness;
                var center = rect.center + Vector2.Scale(direction, radius) + normal * wobble;
                helper.AddVert(center + normal * (thickness * 0.5f + outerJitter), color, Vector2.zero);
                helper.AddVert(center - normal * (thickness * 0.5f + innerJitter), color, Vector2.zero);
                if (i == 0)
                    continue;
                var index = helper.currentVertCount - 4;
                helper.AddTriangle(index, index + 2, index + 3);
                helper.AddTriangle(index, index + 3, index + 1);
            }
        }
    }
}
