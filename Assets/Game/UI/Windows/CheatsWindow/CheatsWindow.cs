using System;
using System.Linq;
using Game.Boosters;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using TMPro;
using UnityEngine;

namespace Game.UI.Windows.CheatsWindow
{
    public class CheatsWindow : Window<CheatsWindowModel>
    {
        [Header("Level")]
        [SerializeField] private TMP_InputField _levelInputField;
        [SerializeField] private GameButton _goButton;

        [Header("Money")]
        [SerializeField] private TMP_InputField _moneyInputField;
        [SerializeField] private GameButton _getMoneyButton;

        [Header("Boosters")]
        [SerializeField] private TMP_InputField _boostersCountInputField;
        [SerializeField] private TMP_Dropdown _boosterKindDropdown;
        [SerializeField] private GameButton _getBoostersButton;


        [Header("Close")]
        [SerializeField] private GameButton _closeButton;

        private CheatsWindowModel _model;

        protected override void Setup(CheatsWindowModel model)
        {
            _model = model;

            _goButton.ClickEvent += OnGoClicked;
            _getMoneyButton.ClickEvent += OnGetMoneyClicked;
            _getBoostersButton.ClickEvent += OnGetBoostersClicked;
            _closeButton.ClickEvent += Close;



            SetupBoosterDropdown();
        }

        private void SetupBoosterDropdown()
        {
            var kinds = Enum.GetValues(typeof(BoosterKind))
                .Cast<BoosterKind>()
                .Where(k => k == BoosterKind.Compass || k == BoosterKind.FreezeTimer)
                .ToList();

            _boosterKindDropdown.ClearOptions();
            _boosterKindDropdown.AddOptions(kinds.Select(k => k.ToString()).ToList());
        }

        private void OnGoClicked()
        {
            if (int.TryParse(_levelInputField.text, out var level))
            {
                _model.GoToLevelAction?.Invoke(level);
            }
        }

        private void OnGetMoneyClicked()
        {
            if (int.TryParse(_moneyInputField.text, out var money))
            {
                _model.AddMoneyAction?.Invoke(money);
            }
        }

        private void OnGetBoostersClicked()
        {
            if (int.TryParse(_boostersCountInputField.text, out var count))
            {
                var selectedKindName = _boosterKindDropdown.options[_boosterKindDropdown.value].text;
                if (Enum.TryParse<BoosterKind>(selectedKindName, out var kind))
                {
                    _model.AddBoostersAction?.Invoke(kind, count);
                }
            }
        }

        private void Close()
        {
            _model.CloseAction?.Invoke();
        }
    }
}
