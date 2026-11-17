using System;
using Cysharp.Threading.Tasks;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using TMPro;
using UnityEngine;

namespace Game.UI.Windows.TimeLoseWindow
{
    public class TimeLoseWindow : Window<TimeLoseWindowModel>
    {
        [SerializeField] private GameButton _adsButton;
        [SerializeField] private GameButton _payAndContinueButton;
        [SerializeField] private TMP_Text _currencyAmountTmp;
        [SerializeField] private GameButton _closeButton;

        private Action _restartAction;
        private Action _payAndContinueAction;
        private Func<UniTask<bool>> _adsAction;
        private bool _canPayAndContinue;

        
        protected override void Setup(TimeLoseWindowModel model)
        {
            _restartAction = model.RestartAction;
            _payAndContinueAction = model.PayAndContinueAction;
            _adsAction = model.AdsAction;
            _canPayAndContinue = model.CanPayAndContinue;
            
            _currencyAmountTmp.text = model.CurrencyPriceValue.ToString();

            _closeButton.ClickEvent += Restart;
            _payAndContinueButton.ClickEvent += PayAndContinue;
            _adsButton.ClickEvent += ShowAds;

            _payAndContinueButton.SetInteractable(_canPayAndContinue);
        }

        private void Restart()
        {
            _restartAction?.Invoke();
        }

        private void PayAndContinue()
        {
            _payAndContinueAction?.Invoke();
        }

        private void ShowAds()
        {
            ShowAdsAsync().Forget();
        }

        private async UniTaskVoid ShowAdsAsync()
        {
            SetButtonsInteractable(false);

            var success = false;
            if (_adsAction != null)
                success = await _adsAction.Invoke();

            if (!success)
                SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _adsButton.SetInteractable(interactable);
            _closeButton.SetInteractable(interactable);
            _payAndContinueButton.SetInteractable(interactable && _canPayAndContinue);
        }
    }
}
