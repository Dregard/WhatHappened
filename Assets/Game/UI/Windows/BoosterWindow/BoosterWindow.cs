using System;
using Cysharp.Threading.Tasks;
using Game.Anomalies;
using Game.Boosters;
using Game.Purchase;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.BoosterWindow
{
    public class BoosterWindow : Window<BoosterWindowModel>
    {
        [SerializeField] private TMP_Text _headerTmp;
        [SerializeField] private TMP_Text _descriptionTmp;
        [SerializeField] private TMP_Text _amountTmp;
        [SerializeField] private Image _iconImage;
        [SerializeField] private GameButtonWithText _buyButton;
        [SerializeField] private GameButton _closeButton;
        [SerializeField] private GameButton _adsButton;
        
        private RectTransform _themedRoot;
        private Button _themedBuy;
        private Button _themedAds;
        private Button _themedClose;
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
            _headerTmp.text = boosterModel.Name;
            _descriptionTmp.text = boosterModel.Description;
            _amountTmp.text = $"+{boosterModel.Reward.Amount}";
            _iconImage.sprite = boosterModel.Icon;
            
            var currencyData = (CurrencyPriceData)boosterModel.Price;
            _buyButton.SetText(currencyData.Amount.ToString());
            _buyButton.SetInteractable(_canBuyBooster);
            
            _buyButton.ClickEvent += BuyBooster;
            _closeButton.ClickEvent += Close;
            _adsButton.ClickEvent += ShowAds;
            BuildThemedPopup(boosterModel, currencyData.Amount);
        }

        /// <summary>Replaces the prefab's old content with the themed out-of-stock popup; buttons reuse the window's purchase, ad and close actions.</summary>
        private void BuildThemedPopup(BoosterViewModel booster, int price)
        {
            var theme = UiThemeData.LoadOrDefault();
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
            if (_themedRoot != null)
                Destroy(_themedRoot.gameObject);
            var isPhoto = booster.Reward != null && booster.Reward.BoosterKind == BoosterKind.FreezeTimer;
            var veil = UiKit.Image("ThemedPopup", transform, null);
            veil.enabled = true;
            veil.color = theme.VeilColor;
            veil.raycastTarget = true;
            _themedRoot = veil.rectTransform;
            var body = UiKit.Image("Body", veil.transform, theme.PopupBody, true);
            UiKit.PlacePadded(body.rectTransform, 80, 480, 1000, 1440, UiKit.BodyPad);
            var ribbon = UiKit.Image("Ribbon", veil.transform, theme.RibbonRed);
            UiKit.PlacePadded(ribbon.rectTransform, 130, 418, 950, 570, UiKit.RibbonPad);
            UiKit.FillLabel(theme, ribbon.transform, isPhoto ? "OUT OF PHOTOS" : "OUT OF HINTS", UiKit.TextStyleAlias.Light, 80,
                new Vector2(0.14f, 0.22f), new Vector2(0.86f, 0.72f));
            var pack = UiKit.Image("Pack", veil.transform, isPhoto ? theme.CameraPackIcon : theme.HintPackIcon);
            UiKit.Place(pack.rectTransform, 325, 545, 755, 975);
            UiKit.Label(theme, "Amount", veil.transform, $"x{booster.Reward.Amount}", UiKit.TextStyleAlias.Light, 585, 800, 720, 890, 100);
            UiKit.Label(theme, "Description", veil.transform, booster.Description, UiKit.TextStyleAlias.Dark, 120, 965, 960, 1040, 52);
            var buy = UiKit.Button(theme, "Buy", veil.transform, theme.ButtonGreen, null, 268, 1068, 812, 1215, BuyBooster, 80, UiKit.ButtonPad);
            buy.interactable = _canBuyBooster;
            UiKit.PlaceRelative(UiKit.Image("Coin", buy.transform, theme.CoinIcon).rectTransform, 261, 1063, 819, 1230, 425, 1085, 530, 1198);
            UiKit.PlaceRelative(UiKit.Label(theme, "Price", buy.transform, price.ToString(), UiKit.TextStyleAlias.Light, 0, 0, 10, 10, 90).rectTransform,
                261, 1063, 819, 1230, 545, 1085, 765, 1198);
            var ads = UiKit.Button(theme, "Ads", veil.transform, theme.ButtonPurple, null, 268, 1245, 812, 1395, ShowAds, 80, UiKit.ButtonPad);
            UiKit.PlaceRelative(UiKit.Image("AdsIcon", ads.transform, theme.AdsIcon).rectTransform, 261, 1240, 819, 1410, 340, 1262, 445, 1378);
            UiKit.PlaceRelative(UiKit.Label(theme, "Free", ads.transform, "+1 FREE", UiKit.TextStyleAlias.Light, 0, 0, 10, 10, 90).rectTransform,
                261, 1240, 819, 1410, 460, 1258, 790, 1382);
            _themedBuy = buy;
            _themedAds = ads;
            _themedClose = UiKit.Button(theme, "Close", veil.transform, theme.CloseIcon, null, 935, 470, 1025, 555, Close);
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
            _adsButton.SetInteractable(interactable);
            _closeButton.SetInteractable(interactable);
            _buyButton.SetInteractable(interactable && _canBuyBooster);
            if (_themedBuy != null)
            {
                _themedBuy.interactable = interactable && _canBuyBooster;
                _themedAds.interactable = interactable;
                _themedClose.interactable = interactable;
            }
        }
    }
}
