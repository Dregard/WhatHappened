using System;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.BoosterUnlockWindow
{
    /// <summary>Themed unlock popup laid out in the BoosterUnlockWindow prefab; the window fills the booster's name, description and icon and routes the OK action.</summary>
    public class BoosterUnlockWindow : Window<BoosterUnlockWindowModel>
    {
        [SerializeField] private TMP_Text _nameTmp;
        [SerializeField] private TMP_Text _descriptionTmp;
        [SerializeField] private Image _iconImage;
        [SerializeField] private GameButton _okButton;

        private Action _closeAction;


        protected override void Setup(BoosterUnlockWindowModel model)
        {
            _closeAction = model.CloseAction;

            var boosterModel = model.BoosterViewModel;
            _nameTmp.text = boosterModel.Name;
            _descriptionTmp.text = boosterModel.Description;
            _iconImage.sprite = boosterModel.Icon;
            _iconImage.preserveAspect = true;

            _okButton.ClickEvent -= Close;
            _okButton.ClickEvent += Close;
        }

        private void Close()
        {
            _closeAction?.Invoke();
        }
    }
}
