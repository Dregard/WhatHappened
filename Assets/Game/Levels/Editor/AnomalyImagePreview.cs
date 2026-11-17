using System.Collections.Generic;
using Game.Anomalies;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.Editor
{
    public sealed class AnomalyImagePreview
    {
        private int _dragIndex = -1;
        private Vector2 _dragStart;
        private Rect _initialBounds;

        /// <summary>Maps bottom-left normalized runtime bounds into the top-left editor image rectangle without rounding.</summary>
        public static Rect ToPreviewRect(Rect picture, Rect bounds)
        {
            return new Rect(picture.x + bounds.x * picture.width, picture.y + (1 - bounds.yMax) * picture.height,
                bounds.width * picture.width, bounds.height * picture.height);
        }

        /// <summary>Draws whole sprites and handles one undoable drag; resizing the window never changes authored coordinates.</summary>
        public bool Draw(Rect area, Sprite mainImage, List<AnomalyData> anomaliesList,
            IReadOnlyList<AnomalyData> visibleList, ref int selectedIndex, Object owner)
        {
            EditorGUI.DrawRect(area, new Color(0.12f, 0.12f, 0.12f));
            if (mainImage == null)
            {
                GUI.Label(area, "Assign Main Image to compose the level.");
                return false;
            }
            var picture = Fit(area, mainImage.rect.size);
            DrawSprite(picture, mainImage);
            foreach (var anomaly in visibleList)
            {
                if (anomaly == null)
                    continue;
                var rect = ToPreviewRect(picture, anomaly.Bounds);
                if (!IsDrawable(rect))
                    continue;
                DrawSprite(rect, anomaly.Image);
            }
            if (selectedIndex >= 0 && selectedIndex < anomaliesList.Count &&
                anomaliesList[selectedIndex] is { } selected && Contains(visibleList, selected))
            {
                var rect = ToPreviewRect(picture, selected.Bounds);
                if (IsDrawable(rect))
                {
                    Handles.BeginGUI();
                    Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.yellow);
                    Handles.EndGUI();
                    GUI.Label(new Rect(rect.x, rect.y - 18, 250, 18), selected.Id);
                }
            }
            return HandleDrag(picture, anomaliesList, visibleList, ref selectedIndex, owner);
        }

        /// <summary>Fits the original sprite aspect to the available canvas, matching the runtime FitInParent behavior.</summary>
        private static Rect Fit(Rect area, Vector2 size)
        {
            var scale = Mathf.Min(area.width / size.x, area.height / size.y);
            return new Rect(area.center - size * scale * 0.5f, size * scale);
        }

        /// <summary>Draws the sprite with its atlas UVs stretched to the full authored rectangle, as in the runtime Image.</summary>
        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null)
                return;
            var padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
            var size = sprite.rect.size;
            var visible = new Rect(rect.x + rect.width * padding.x / size.x,
                rect.y + rect.height * padding.w / size.y,
                rect.width * (1 - (padding.x + padding.z) / size.x),
                rect.height * (1 - (padding.y + padding.w) / size.y));
            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            GUI.DrawTextureWithTexCoords(visible, sprite.texture, new Rect(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y), true);
        }

        /// <summary>Ignores malformed geometry in the preview; diagnostics retain and report the authored values.</summary>
        private static bool IsDrawable(Rect rect)
        {
            return rect.width > 0 && rect.height > 0 && float.IsFinite(rect.x) && float.IsFinite(rect.y) &&
                float.IsFinite(rect.width) && float.IsFinite(rect.height);
        }

        /// <summary>Checks layer visibility by reference so duplicate authored IDs remain individually editable.</summary>
        private static bool Contains(IReadOnlyList<AnomalyData> visibleList, AnomalyData anomaly)
        {
            for (var i = 0; i < visibleList.Count; i++)
                if (ReferenceEquals(visibleList[i], anomaly))
                    return true;
            return false;
        }

        /// <summary>Captures the topmost layer and preserves subpixel displacement; one drag forms one Unity Undo operation.</summary>
        private bool HandleDrag(Rect picture, List<AnomalyData> anomaliesList,
            IReadOnlyList<AnomalyData> visibleList, ref int selectedIndex, Object owner)
        {
            var current = Event.current;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (current.type == EventType.MouseDown && current.button == 0 && picture.Contains(current.mousePosition))
            {
                for (var i = anomaliesList.Count - 1; i >= 0; i--)
                {
                    var anomaly = anomaliesList[i];
                    if (anomaly == null || !Contains(visibleList, anomaly) ||
                        !ToPreviewRect(picture, anomaly.Bounds).Contains(current.mousePosition))
                        continue;
                    selectedIndex = _dragIndex = i;
                    _dragStart = current.mousePosition;
                    _initialBounds = anomaly.Bounds;
                    Undo.RegisterCompleteObjectUndo(owner, "Move anomaly image");
                    GUIUtility.hotControl = controlId;
                    current.Use();
                    return true;
                }
            }
            if (GUIUtility.hotControl != controlId || _dragIndex < 0 || _dragIndex >= anomaliesList.Count)
                return false;
            if (current.type == EventType.MouseDrag)
            {
                var delta = current.mousePosition - _dragStart;
                var bounds = _initialBounds;
                bounds.position += new Vector2(delta.x / picture.width, -delta.y / picture.height);
                anomaliesList[_dragIndex].Bounds = bounds;
                EditorUtility.SetDirty(owner);
                current.Use();
                return true;
            }
            if (current.type == EventType.MouseUp && current.button == 0)
            {
                _dragIndex = -1;
                GUIUtility.hotControl = 0;
                current.Use();
                return true;
            }
            return false;
        }
    }
}
