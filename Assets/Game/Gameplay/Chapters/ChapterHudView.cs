using System;
using Game.UI.Buttons;
using TMPro;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Location title plate and tappable evidence counter laid out in the ChapterHud prefab.</summary>
    public sealed class ChapterHudView : MonoBehaviour
    {
        [SerializeField] private GameObject _titlePlate;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private GameButton _button;

        private Action _investigate;

        /// <summary>Fills the location title (the plate hides without one) and connects the evidence counter to the investigation.</summary>
        public void Initialize(string title, Action investigate)
        {
            _titlePlate.SetActive(!string.IsNullOrEmpty(title));
            if (!string.IsNullOrEmpty(title))
                _titleLabel.text = title.ToUpperInvariant();
            _investigate = investigate;
            _button.ClickEvent -= OnClick;
            _button.ClickEvent += OnClick;
        }

        private void OnClick()
        {
            _investigate?.Invoke();
        }

        /// <summary>Refreshes the supplied found/total count and interaction availability.</summary>
        public void Show(int count, int total, bool interactable)
        {
            gameObject.SetActive(true);
            _count.text = $"{count}/{total}";
            _button.SetInteractable(interactable);
        }
    }
}
