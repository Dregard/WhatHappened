using Game.Anomalies;
using Game.Boosters;
using Game.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Hud
{
    /// <summary>Themed gold counter and Hint/Photo booster widgets placed as in the mockups; state comes only from existing systems.</summary>
    public sealed class HudLayoutView : MonoBehaviour
    {
        private sealed class BoosterWidget
        {
            public BoosterKind Kind;
            public Button Button;
            public CanvasGroup Group;
            public TMP_Text Count;
        }

        private UiThemeData _theme;
        private HudModel _model;
        private CurrencySystem _currency;
        private TMP_Text _gold;
        private BoosterWidget _hint;
        private BoosterWidget _photo;
        private readonly System.Collections.Generic.List<GameObject> _photoObjectsList = new();

        /// <summary>Creates the layout above the HUD canvas content and subscribes to balance and stock changes.</summary>
        public static HudLayoutView Create(Canvas hudCanvas, UiThemeData theme, HudModel model)
        {
            var root = new GameObject("HudLayout", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.layer = hudCanvas.gameObject.layer;
            root.transform.SetParent(hudCanvas.transform, false);
            AnomalyUi.Bounds((RectTransform)root.transform, Vector2.zero, Vector2.one);
            var canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = hudCanvas.sortingLayerID;
            canvas.sortingOrder = hudCanvas.sortingOrder + 1;
            var view = root.AddComponent<HudLayoutView>();
            view.Build(theme, model);
            return view;
        }

        /// <summary>Builds gold plus both booster widgets and binds them to stock and unlock state.</summary>
        private void Build(UiThemeData theme, HudModel model)
        {
            _theme = theme;
            _model = model;
            var plate = UiKit.Image("GoldPlate", transform, theme.PlateCounter, true);
            UiKit.Place(plate.rectTransform, 95, 218, 265, 272);
            UiKit.Place(UiKit.Image("GoldIcon", transform, theme.CoinIcon).rectTransform, 40, 200, 130, 290);
            _gold = UiKit.Label(theme, "GoldValue", transform, "0", UiKit.TextStyleAlias.Light, 140, 218, 260, 272, 44);
            if (model.Container != null && model.Container.HasBinding<CurrencySystem>())
            {
                _currency = model.Container.Resolve<CurrencySystem>();
                _currency.CurrencyChangedEvent += HandleCurrencyChanged;
                _gold.text = _currency.GetCurrencyValue(CurrencyKind.Gold).ToString();
            }

            var hintPlate = UiKit.Image("HintPlate", transform, theme.PlateCounter, true);
            UiKit.Place(hintPlate.rectTransform, 95, 337, 265, 391);
            UiKit.Place(UiKit.Image("HintIcon", transform, theme.HintIcon).rectTransform, 40, 298, 150, 414);
            _hint = BuildWidget("Hint", BoosterKind.Compass, 40, 298, 270, 414, UiKit.TextStyleAlias.Light, 150, 337, 215, 391);
            var plus = UiKit.Image("HintPlus", transform, theme.BadgeGreen);
            UiKit.Place(plus.rectTransform, 218, 340, 266, 388);
            UiKit.FillLabel(theme, plus.transform, "+", UiKit.TextStyleAlias.Light, 40);

            var cameraIcon = UiKit.Image("CameraIcon", transform, theme.CameraIcon);
            UiKit.Place(cameraIcon.rectTransform, 683, 270, 750, 332);
            var badge = UiKit.Image("PhotoBadge", transform, theme.BadgeGreen);
            _photoObjectsList.Add(cameraIcon.gameObject);
            _photoObjectsList.Add(badge.gameObject);
            UiKit.Place(badge.rectTransform, 710, 312, 772, 372);
            _photo = BuildWidget("Photo", BoosterKind.FreezeTimer, 680, 266, 776, 376, UiKit.TextStyleAlias.Light, 710, 312, 772, 372);
            _photoObjectsList.Add(_photo.Button.gameObject);
            _photo.Count.transform.SetParent(badge.transform, false);
            AnomalyUi.Bounds(_photo.Count.rectTransform, Vector2.zero, Vector2.one);
            _model.SubscribeOnBoosterCountChanged?.Invoke(HandleBoosterCountChanged);
            Refresh();
        }

        /// <summary>Creates a transparent button covering a widget and a count label inside given mockup bounds.</summary>
        private BoosterWidget BuildWidget(string name, BoosterKind kind, float left, float top, float right, float bottom,
            UiKit.TextStyleAlias style, float countLeft, float countTop, float countRight, float countBottom)
        {
            var hit = UiKit.Image(name + "Button", transform, null);
            hit.enabled = true;
            hit.color = Color.clear;
            hit.raycastTarget = true;
            UiKit.Place(hit.rectTransform, left, top, right, bottom);
            var widget = new BoosterWidget { Kind = kind, Button = hit.gameObject.AddComponent<Button>() };
            widget.Button.targetGraphic = hit;
            widget.Group = hit.gameObject.AddComponent<CanvasGroup>();
            widget.Group.ignoreParentGroups = true;
            widget.Button.onClick.AddListener(() => _model.UseBoosterAction?.Invoke(kind));
            widget.Count = UiKit.Label(_theme, name + "Count", transform, "0", style, countLeft, countTop, countRight, countBottom, 44);
            return widget;
        }

        /// <summary>Updates the displayed gold when the shared balance changes.</summary>
        private void HandleCurrencyChanged(CurrencyKind kind, int amount)
        {
            if (kind == CurrencyKind.Gold && _gold != null)
                _gold.text = amount.ToString();
        }

        /// <summary>Shows the latest stock of a booster and refreshes its availability.</summary>
        private void HandleBoosterCountChanged(BoosterKind kind, int count)
        {
            Refresh();
        }

        /// <summary>Reads stock and gates from the model; an empty stock stays tappable because it opens the purchase popup.</summary>
        public void Refresh()
        {
            Apply(_hint);
            Apply(_photo);
            var isPhotoShown = _model.IsSearchStartedFunc == null || _model.IsSearchStartedFunc();
            foreach (var photoObject in _photoObjectsList)
                if (photoObject != null && photoObject.activeSelf != isPhotoShown)
                    photoObject.SetActive(isPhotoShown);
        }

        /// <summary>Applies one widget's count and enabled look from the authoritative booster state.</summary>
        private void Apply(BoosterWidget widget)
        {
            if (widget == null || _model.GetBoosterCountFunc == null)
                return;
            widget.Count.text = _model.GetBoosterCountFunc(widget.Kind).ToString();
            var isUnlocked = _model.IsBoosterUnlockedFunc == null || _model.IsBoosterUnlockedFunc(widget.Kind);
            var canUse = isUnlocked && (_model.CanUseBoosterFunc == null || _model.CanUseBoosterFunc(widget.Kind));
            widget.Button.interactable = canUse;
            widget.Group.alpha = canUse ? 1f : 0.55f;
        }

        /// <summary>Releases the balance subscription.</summary>
        private void OnDestroy()
        {
            if (_currency != null)
                _currency.CurrencyChangedEvent -= HandleCurrencyChanged;
        }
    }
}
