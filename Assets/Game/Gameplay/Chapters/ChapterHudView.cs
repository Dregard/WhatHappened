using System;
using Game.Anomalies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    public sealed class ChapterHudView : MonoBehaviour
    {
        private TMP_Text _count;
        private Button _button;

        /// <summary>Creates the location title plate and the tappable evidence counter on the existing HUD canvas.</summary>
        public void Initialize(string title, Action investigate)
        {
            var theme = UiThemeData.LoadOrDefault();
            if (!string.IsNullOrEmpty(title))
            {
                var plate = UiKit.Image("TitlePlate", transform, theme.PlateChapter, true);
                UiKit.Place(plate.rectTransform, 365, 52, 715, 118);
                UiKit.FillLabel(theme, plate.transform, title.ToUpperInvariant(), UiKit.TextStyleAlias.Light, 46);
            }
            var counterPlate = UiKit.Image("CluesPlate", transform, theme.PlateCounter, true);
            UiKit.Place(counterPlate.rectTransform, 95, 105, 265, 160);
            var icon = UiKit.Image("CluesIcon", transform, theme.ClueCounterIcon);
            UiKit.Place(icon.rectTransform, 40, 90, 130, 180);
            _count = UiKit.Label(theme, "CluesValue", transform, "0/0", UiKit.TextStyleAlias.Light, 140, 105, 260, 160, 44);
            var hit = UiKit.Image("CluesButton", transform, null);
            hit.enabled = true;
            hit.color = Color.clear;
            hit.raycastTarget = true;
            UiKit.Place(hit.rectTransform, 40, 90, 270, 180);
            _button = hit.gameObject.AddComponent<Button>();
            _button.targetGraphic = hit;
            _button.transition = Selectable.Transition.None;
            _button.onClick.AddListener(() => investigate());
        }

        /// <summary>Refreshes the supplied found/total count and interaction availability.</summary>
        public void Show(int count, int total, bool interactable)
        {
            gameObject.SetActive(true);
            _count.text = $"{count}/{total}";
            _button.interactable = interactable;
        }
    }
}
