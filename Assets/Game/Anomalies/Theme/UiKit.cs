using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Anomalies
{
    /// <summary>Builds themed passive/interactive UI primitives positioned in the 1080x1920 mockup coordinate space.</summary>
    public static class UiKit
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        /// <summary>Text styles of the delivered mockups.</summary>
        public enum TextStyleAlias
        {
            Light = 0,
            Dark = 1,
            Gold = 2,
        }

        /// <summary>Converts a mockup pixel rectangle (origin top-left) to normalized anchors and applies it.</summary>
        public static void Place(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = new Vector2(left / ReferenceWidth, 1 - bottom / ReferenceHeight);
            rect.anchorMax = new Vector2(right / ReferenceWidth, 1 - top / ReferenceHeight);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // Transparent shadow margins (left, top, right, bottom in sprite pixels) baked into the delivered art; padding keeps the visible edge on the mockup rectangle.
        public static readonly Vector4 BodyPad = new(20f, 15f, 20f, 30f);
        public static readonly Vector4 FramePad = new(20f, 14f, 28f, 30f);
        public static readonly Vector4 ButtonPad = new(7f, 5f, 7f, 15f);
        public static readonly Vector4 RibbonPad = new(20f, 28f, 20f, 25f);

        /// <summary>Places sprite art so its visible edge, not its shadow margin, lies on the given mockup rectangle.</summary>
        public static void PlacePadded(RectTransform rect, float left, float top, float right, float bottom, Vector4 pad)
        {
            Place(rect, left - pad.x, top - pad.y, right + pad.z, bottom + pad.w);
        }

        /// <summary>Places a child by a pixel rectangle measured inside a parent whose own mockup rectangle is given.</summary>
        public static void PlaceRelative(RectTransform rect, float parentLeft, float parentTop, float parentRight, float parentBottom,
            float left, float top, float right, float bottom)
        {
            PlaceIn(rect, parentRight - parentLeft, parentBottom - parentTop, left - parentLeft, top - parentTop, right - parentLeft, bottom - parentTop);
        }

        /// <summary>Places a child by pixel rectangle inside a parent of the given pixel size (origin top-left).</summary>
        public static void PlaceIn(RectTransform rect, float parentWidth, float parentHeight, float left, float top, float right, float bottom)
        {
            rect.anchorMin = new Vector2(left / parentWidth, 1 - bottom / parentHeight);
            rect.anchorMax = new Vector2(right / parentWidth, 1 - top / parentHeight);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Converts a found slot into its themed completed look: green frame, the found picture and a check mark.</summary>
        public static void MarkFound(Image slot, Sprite found)
        {
            var theme = UiThemeData.LoadOrDefault();
            slot.sprite = theme.SlotFoundFrame;
            slot.color = Color.white;
            slot.enabled = theme.SlotFoundFrame != null;
            if (slot.transform.childCount > 0)
                slot.transform.GetChild(0).gameObject.SetActive(false);
            var picture = Image("FoundPicture", slot.transform, found);
            Place(picture.rectTransform, 0, 0, ReferenceWidth, ReferenceHeight);
            picture.rectTransform.anchorMin = new Vector2(0.12f, 0.12f);
            picture.rectTransform.anchorMax = new Vector2(0.88f, 0.88f);
            var check = Image("Check", slot.transform, theme.CheckIcon);
            check.rectTransform.anchorMin = new Vector2(0.45f, -0.08f);
            check.rectTransform.anchorMax = new Vector2(1.05f, 0.45f);
            check.rectTransform.offsetMin = check.rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>Creates a themed image; sliced sprites stretch, others keep their aspect.</summary>
        public static Image Image(string name, Transform parent, Sprite sprite, bool sliced = false)
        {
            var image = AnomalyUi.Image(name, parent, sprite);
            image.type = sliced && sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.enabled = sprite != null;
            return image;
        }

        /// <summary>Creates a themed label inside mockup pixel bounds. Auto-size keeps text within its box.</summary>
        public static TMP_Text Label(UiThemeData theme, string name, Transform parent, string text, TextStyleAlias style,
            float left, float top, float right, float bottom, float maxSize = 60f)
        {
            var label = AnomalyUi.Text(name, parent, Vector2.zero, Vector2.one);
            Place(label.rectTransform, left, top, right, bottom);
            Style(theme, label, style);
            label.text = text;
            label.fontSizeMax = maxSize;
            label.fontSizeMin = 10;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        /// <summary>Applies the theme font, color and outline to a label.</summary>
        public static void Style(UiThemeData theme, TMP_Text label, TextStyleAlias style)
        {
            if (theme.Font != null)
                label.font = theme.Font;
            label.fontStyle = theme.BoldText ? FontStyles.Bold : FontStyles.Normal;
            label.characterSpacing = theme.BoldText ? theme.BoldCharacterSpacing : 0f;
            switch (style)
            {
                case TextStyleAlias.Dark:
                    label.color = theme.DarkTextColor;
                    label.outlineWidth = 0;
                    break;
                case TextStyleAlias.Gold:
                    label.color = theme.GoldTextColor;
                    label.outlineColor = theme.GoldOutlineColor;
                    label.outlineWidth = theme.OutlineWidth;
                    break;
                default:
                    label.color = theme.LightTextColor;
                    label.outlineColor = theme.LightOutlineColor;
                    label.outlineWidth = theme.OutlineWidth;
                    break;
            }
        }

        /// <summary>Creates a sprite button with an optional centered label; the sprite itself receives the raycast.</summary>
        public static Button Button(UiThemeData theme, string name, Transform parent, Sprite sprite, string text,
            float left, float top, float right, float bottom, Action onClick, float labelSize = 80f, Vector4? pad = null)
        {
            var image = Image(name, parent, sprite, true);
            PlacePadded(image.rectTransform, left, top, right, bottom, pad ?? Vector4.zero);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            if (!string.IsNullOrEmpty(text))
                FillLabel(theme, image.transform, text, TextStyleAlias.Light, labelSize);
            return button;
        }

        /// <summary>Stretches a label to fill its parent, for buttons and plates of arbitrary size.</summary>
        public static TMP_Text FillLabel(UiThemeData theme, Transform parent, string text, TextStyleAlias style, float maxSize = 60f,
            Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            var label = AnomalyUi.Text("Label", parent, anchorMin ?? Vector2.zero, anchorMax ?? Vector2.one);
            Style(theme, label, style);
            label.text = text;
            label.fontSizeMax = maxSize;
            label.fontSizeMin = 10;
            label.margin = new Vector4(8, 0, 8, 0);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        /// <summary>Creates a transparent rectangle used only to group children and place them in mockup space.</summary>
        public static RectTransform Group(string name, Transform parent, float left, float top, float right, float bottom)
        {
            var image = AnomalyUi.Image(name, parent);
            image.color = Color.clear;
            image.enabled = false;
            Place(image.rectTransform, left, top, right, bottom);
            return image.rectTransform;
        }
    }
}
