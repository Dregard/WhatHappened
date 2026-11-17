using Game.Anomalies;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    /// <summary>One evidence circle: a dashed question mark until found, then a gold ring with its icon on a cream disc.</summary>
    public sealed class ChapterEvidenceSlotView
    {
        private readonly Image _empty;
        private readonly Image _discBacking;
        private readonly Image _disc;
        private readonly Image _ring;
        private readonly Image _icon;
        public RectTransform Rect { get; private set; }

        /// <summary>Creates a slot centered at a mockup point; an icon marks it found immediately.</summary>
        public ChapterEvidenceSlotView(UiThemeData theme, Transform parent, string name, float centerX, float centerY, float diameter, Sprite icon)
        {
            var root = UiKit.Group(name, parent, centerX - diameter * 0.5f, centerY - diameter * 0.5f, centerX + diameter * 0.5f, centerY + diameter * 0.5f);
            Rect = root;
            _empty = UiKit.Image("Empty", root, theme.EvidenceEmpty);
            UiKit.FillLabel(theme, _empty.transform, "?", UiKit.TextStyleAlias.Light, 160, new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.88f));
            // The cream disc art is slightly translucent; a second copy makes it opaque like the mockup.
            _discBacking = InsetImage("DiscBacking", root, theme.EvidenceDisc, 0.06f);
            _disc = InsetImage("Disc", root, theme.EvidenceDisc, 0.06f);
            _icon = InsetImage("Icon", root, null, 0.2f);
            _ring = UiKit.Image("Ring", root, theme.EvidenceRing);
            SetFound(icon);
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

        /// <summary>Creates an image inset from every edge of its parent by a normalized margin.</summary>
        private static Image InsetImage(string name, Transform parent, Sprite sprite, float margin)
        {
            var image = UiKit.Image(name, parent, sprite);
            image.rectTransform.anchorMin = new Vector2(margin, margin);
            image.rectTransform.anchorMax = new Vector2(1 - margin, 1 - margin);
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }
    }
}
