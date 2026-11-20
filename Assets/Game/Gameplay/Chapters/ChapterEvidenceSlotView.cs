using Game.Anomalies;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    /// <summary>One evidence circle of the ChapterEvidenceSlot prefab: a dashed question mark until found, then a gold ring with its icon on a cream disc.</summary>
    public sealed class ChapterEvidenceSlotView : MonoBehaviour
    {
        [SerializeField] private Image _empty;
        [SerializeField] private Image _discBacking;
        [SerializeField] private Image _disc;
        [SerializeField] private Image _ring;
        [SerializeField] private Image _icon;

        public RectTransform Rect { get { return (RectTransform)transform; } }

        /// <summary>Centers the slot at a mockup point (1080 wide, referenceHeight tall); an icon marks it found immediately.</summary>
        public void Place(float centerX, float centerY, float diameter, Sprite icon, float referenceHeight = UiKit.ReferenceHeight)
        {
            UiKit.PlaceIn(Rect, UiKit.ReferenceWidth, referenceHeight, centerX - diameter * 0.5f, centerY - diameter * 0.5f, centerX + diameter * 0.5f, centerY + diameter * 0.5f);
            SetFound(icon);
        }

        /// <summary>Sets the slot's normalized rectangle directly, with no offsets.</summary>
        public void SetAnchors(Vector2 min, Vector2 max)
        {
            Rect.anchorMin = min;
            Rect.anchorMax = max;
            Rect.offsetMin = Rect.offsetMax = Vector2.zero;
        }

        /// <summary>Shows the found look when an icon is supplied, otherwise the unfound look.</summary>
        public void SetFound(Sprite icon)
        {
            var isFound = icon != null;
            _empty.gameObject.SetActive(!isFound);
            _discBacking.gameObject.SetActive(isFound);
            _disc.gameObject.SetActive(isFound);
            _ring.gameObject.SetActive(isFound);
            _icon.gameObject.SetActive(isFound);
            _icon.sprite = icon;
            _icon.enabled = isFound;
        }
    }
}
