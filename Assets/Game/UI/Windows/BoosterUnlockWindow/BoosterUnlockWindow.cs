using System;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.BoosterUnlockWindow
{
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
            
            _okButton.ClickEvent += Close;
        }

        private void Close()
        {
            _closeAction?.Invoke();
        }
    }
}
