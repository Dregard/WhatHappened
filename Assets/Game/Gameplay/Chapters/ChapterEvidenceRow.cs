using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Anomalies;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Full-screen container that lays out one evidence circle per chapter level from the slot prefab.</summary>
    public sealed class ChapterEvidenceRow : MonoBehaviour
    {
        private const float RowCenterX = 540f;
        private const float GapFactor = 0.35f;

        [SerializeField] private ChapterEvidenceSlotView _slotPrefab;

        private readonly List<ChapterEvidenceSlotView> _slotsList = new();

        public IReadOnlyList<ChapterEvidenceSlotView> Slots { get { return _slotsList; } }

        /// <summary>Rebuilds the row centered horizontally at a mockup height, found evidences first; the diameter shrinks so any length fits the width. The gap between circles is gapFactor of the diameter.</summary>
        public void Build(float centerY, float preferredDiameter, float availableWidth, Sprite[] sprites, float gapFactor = GapFactor, float referenceHeight = UiKit.ReferenceHeight)
        {
            Clear();
            var count = sprites.Length;
            var diameter = count <= 0 ? preferredDiameter : Mathf.Min(preferredDiameter, availableWidth / (count + (count - 1) * gapFactor));
            var gap = diameter * gapFactor;
            var rowWidth = count * diameter + (count - 1) * gap;
            for (var index = 0; index < count; index++)
            {
                var centerX = RowCenterX - rowWidth * 0.5f + diameter * 0.5f + index * (diameter + gap);
                var slot = Instantiate(_slotPrefab, transform, false);
                slot.name = "Evidence" + index;
                slot.Place(centerX, centerY, diameter, sprites[index], referenceHeight);
                _slotsList.Add(slot);
            }
        }

        /// <summary>Rebuilds the row from a layout description.</summary>
        public void Build(ChapterRowLayout layout, Sprite[] sprites)
        {
            Build(layout.CenterY, layout.Diameter, layout.Width, sprites, layout.GapFactor, layout.ReferenceHeight);
        }

        /// <summary>Moves the existing slots to a new layout at once.</summary>
        public void Arrange(ChapterRowLayout layout)
        {
            for (var index = 0; index < _slotsList.Count; index++)
            {
                var anchors = Target(layout, index, _slotsList.Count);
                _slotsList[index].SetAnchors(anchors.Min, anchors.Max);
            }
        }

        /// <summary>Slides the existing slots, keeping their pictures, from where they are to a new layout.</summary>
        public async UniTask MoveTo(ChapterRowLayout layout, float seconds, CancellationToken token)
        {
            var count = _slotsList.Count;
            var from = new ChapterAnchors[count];
            var to = new ChapterAnchors[count];
            for (var index = 0; index < count; index++)
            {
                var rect = _slotsList[index].Rect;
                from[index] = new ChapterAnchors(rect.anchorMin.x, rect.anchorMin.y, rect.anchorMax.x, rect.anchorMax.y);
                to[index] = Target(layout, index, count);
            }
            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.SmoothStep(0, 1, elapsed / seconds);
                for (var index = 0; index < count; index++)
                    _slotsList[index].SetAnchors(Vector2.Lerp(from[index].Min, to[index].Min, t), Vector2.Lerp(from[index].Max, to[index].Max, t));
                await UniTask.NextFrame(token);
            }
            Arrange(layout);
        }

        /// <summary>Normalized anchors of one slot for a layout; mirrors the math of Build.</summary>
        private static ChapterAnchors Target(ChapterRowLayout layout, int index, int count)
        {
            var diameter = count <= 0 ? layout.Diameter : Mathf.Min(layout.Diameter, layout.Width / (count + (count - 1) * layout.GapFactor));
            var gap = diameter * layout.GapFactor;
            var rowWidth = count * diameter + (count - 1) * gap;
            var centerX = RowCenterX - rowWidth * 0.5f + diameter * 0.5f + index * (diameter + gap);
            return new ChapterAnchors(
                (centerX - diameter * 0.5f) / UiKit.ReferenceWidth,
                1 - (layout.CenterY + diameter * 0.5f) / layout.ReferenceHeight,
                (centerX + diameter * 0.5f) / UiKit.ReferenceWidth,
                1 - (layout.CenterY - diameter * 0.5f) / layout.ReferenceHeight);
        }

        /// <summary>Removes the slots of the previous build.</summary>
        public void Clear()
        {
            foreach (var slot in _slotsList)
                if (slot != null)
                    Destroy(slot.gameObject);
            _slotsList.Clear();
        }
    }
}
