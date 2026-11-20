using System;
using Cysharp.Threading.Tasks;
using Game.Boosters;
using Game.Purchase;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.BoosterWindow
{
    /// <summary>Out-of-stock popup laid out in the BoosterWindow prefab; the window fills it with the booster's data and routes the buy, ad and close actions.</summary>
    public class BoosterWindow : Window<BoosterWindowModel>
    {
        [SerializeField] private TMP_Text _headerTmp;
        [SerializeField] private TMP_Text _descriptionTmp;
        [SerializeField] private TMP_Text _amountTmp;
        [SerializeField] private TMP_Text _priceTmp;
        [SerializeField] private Image _packImage;
        [SerializeField] private Sprite _hintPackSprite;
        [SerializeField] private Sprite _photoPackSprite;
        [SerializeField, Range(0.1f, 1f)] private float _photoPackScale = 0.72f;
        [SerializeField] private GameButton _buyButton;
        [SerializeField] private GameButton _closeButton;
        [SerializeField] private GameButton _adsButton;

        private Action _buyAction;
        private Action _closeAction;
        private Func<UniTask<bool>> _adsAction;
        private bool _canBuyBooster;

        protected override void Setup(BoosterWindowModel model)
        {
            _buyAction = model.BuyBoosterAction;
            _closeAction = model.CloseAction;
            _adsAction = model.AdsAction;
            _canBuyBooster = model.CanBuyBooster;

            var boosterModel = model.BoosterViewModel;
            var isPhoto = boosterModel.Reward != null && boosterModel.Reward.BoosterKind == BoosterKind.FreezeTimer;
            _headerTmp.text = isPhoto ? "OUT OF PHOTOS" : "OUT OF HINTS";
            _packImage.sprite = isPhoto ? _photoPackSprite : _hintPackSprite;
            _packImage.rectTransform.localScale = Vector3.one * (isPhoto ? _photoPackScale : 1f);
            _amountTmp.text = $"x{boosterModel.Reward.Amount}";
            _descriptionTmp.text = boosterModel.Description;
            var currencyData = (CurrencyPriceData)boosterModel.Price;
            _priceTmp.text = currencyData.Amount.ToString();
            _buyButton.SetInteractable(_canBuyBooster);

            _buyButton.ClickEvent -= BuyBooster;
            _closeButton.ClickEvent -= Close;
            _adsButton.ClickEvent -= ShowAds;
            _buyButton.ClickEvent += BuyBooster;
            _closeButton.ClickEvent += Close;
            _adsButton.ClickEvent += ShowAds;
        }

        private void BuyBooster()
        {
            _buyAction?.Invoke();
        }

        private void Close()
        {
            _closeAction?.Invoke();
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
            _buyButton.SetInteractable(interactable && _canBuyBooster);
            _adsButton.SetInteractable(interactable);
            _closeButton.SetInteractable(interactable);
        }
    }
}
