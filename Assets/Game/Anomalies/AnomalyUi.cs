using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Anomalies
{
    public static class AnomalyUi
    {
        /// <summary>Creates a passive simple sprite layer; the board alone owns hit input.</summary>
        public static Image Image(string name, Transform parent, Sprite sprite = null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            Bounds(image.rectTransform, Vector2.zero, Vector2.one);
            return image;
        }

        /// <summary>Creates passive centered text within supplied normalized anchors.</summary>
        public static TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<TextMeshProUGUI>();
            Bounds(text.rectTransform, min, max);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Sets normalized anchors without offsets or changing the authored geometry.</summary>
        public static void Bounds(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Sets a prefab's fit-in-parent fitter to a sprite's aspect so the image shares the exact visible rectangle with its children.</summary>
        public static void SetAspect(AspectRatioFitter fitter, Sprite sprite)
        {
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        /// <summary>Returns an image's rendered rectangle in a shared ancestor's local UI coordinates.</summary>
        public static Rect LocalRect(RectTransform image, RectTransform ancestor)
        {
            var min = ancestor.InverseTransformPoint(image.TransformPoint(image.rect.min));
            var max = ancestor.InverseTransformPoint(image.TransformPoint(image.rect.max));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>Places an animation layer in local coordinates, independent of parent anchor changes.</summary>
        public static void Place(RectTransform image, Rect rect)
        {
            image.anchorMin = image.anchorMax = new Vector2(0.5f, 0.5f);
            image.pivot = new Vector2(0.5f, 0.5f);
            image.localPosition = rect.center;
            image.sizeDelta = rect.size;
        }
    }
}
