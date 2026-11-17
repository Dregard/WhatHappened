using System;
using System.Collections.Generic;
using System.Linq;
using Game.Boosters;

namespace Game.UI.Hud
{
    public class BoosterButtonsController
    {
        private readonly List<BoosterButtonView> _buttons;
        private readonly HudModel _model;
        private readonly Dictionary<BoosterKind, BoosterButtonView> _boosterToButtonMap = new();
        
        private Func<BoosterKind, bool> _isBoosterUnlockedFunc;
        

        public BoosterButtonsController(List<BoosterButtonView> buttons, HudModel model, Func<BoosterKind, bool> isBoosterUnlockedFunc)
        {
            _buttons = buttons;
            _model = model;
            _isBoosterUnlockedFunc = isBoosterUnlockedFunc;
        }

        /// <summary>Binds installed effects and their authored presentation, hiding unused legacy slots.</summary>
        public void Initialize()
        {
            if (_model.Boosters == null || _buttons == null) return;

            var boosters = _model.Boosters.ToList();
            
            for (var i = 0; i < _buttons.Count; i++)
            {
                if (i < boosters.Count)
                {
                    var booster = boosters[i];
                    var buttonView = _buttons[i];
                    
                    buttonView.gameObject.SetActive(true);
                    var presentation = booster.GetViewModel();
                    buttonView.SetPresentation(presentation.Icon, presentation.Name);
                    buttonView.Setup(
                        _model.GetBoosterCountFunc(booster.Kind), 
                        _isBoosterUnlockedFunc(booster.Kind),
                        booster.OpenLevel + 1,
                        () => _model.UseBoosterAction(booster.Kind)
                    );
                    
                    _boosterToButtonMap[booster.Kind] = buttonView;
                }
                else
                {
                    _buttons[i].gameObject.SetActive(false);
                }
            }

            _model.SubscribeOnBoosterCountChanged?.Invoke(HandleBoosterCountChanged);
        }

        /// <summary>Refreshes stock without briefly re-enabling a button blocked by its just-started effect.</summary>
        private void HandleBoosterCountChanged(BoosterKind kind, int count)
        {
            if (_boosterToButtonMap.TryGetValue(kind, out var button))
            {
                button.SetLocked(!_isBoosterUnlockedFunc(kind));
                button.UpdateCount(count);
                button.SetInteractable(_isBoosterUnlockedFunc(kind) && _model.CanUseBoosterFunc(kind));
            }
        }

        /// <summary>Refreshes pause, photo allowance and active-target gates even when inventory has not changed.</summary>
        public void RefreshButtonsInteractable()
        {
            foreach (var pair in _boosterToButtonMap)
            {
                var kind = pair.Key;
                var button = pair.Value;
                
                button.SetInteractable(_isBoosterUnlockedFunc(kind) && _model.CanUseBoosterFunc(kind));
            }
        }
    }
}
