using System;
using Game.UI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Hud
{
    public class BoosterButtonView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private GameButton _button;
        [SerializeField] private GameObject _lockedGameObject;
        [SerializeField] private GameObject _unlockedGameObject;
        [SerializeField] private TMP_Text _unlockLevelTmp;
        [SerializeField] private string _unlockLevelFormat;

        private Action _onClick;

        /// <summary>Uses the shared booster presentation rather than the old slot's fixed puzzle icon.</summary>
        public void SetPresentation(Sprite icon, string title)
        {
            var iconTransform = _unlockedGameObject.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent<Image>(out var image))
            {
                image.sprite = icon;
                image.preserveAspect = true;
            }
            gameObject.name = title;
        }


        /// <summary>Rebinds one button without accumulating click callbacks when HUD setup is repeated.</summary>
        public void Setup(int count, bool isUnlocked, int unlockLevel, Action onClick)
        {
            UpdateCount(count);
            SetLocked(!isUnlocked);
            
            _unlockLevelTmp.text = string.Format(_unlockLevelFormat, unlockLevel);
            if (_onClick != null)
                _button.ClickEvent -= _onClick;
            _onClick = onClick;
            _button.ClickEvent += _onClick;
            _button.SetInteractable(isUnlocked);
        }

        public void UpdateCount(int count)
        {
            _countText.text = count > 0 ? count.ToString() : "+";
        }

        public void SetLocked(bool isLocked)
        {
            _button.SetInteractable(isLocked == false);
            _lockedGameObject.SetActive(isLocked);
            _unlockedGameObject.SetActive(!isLocked);
        }

        public void SetInteractable(bool isInteractable)
        {
            _button.SetInteractable(isInteractable);
        }
    }
}
