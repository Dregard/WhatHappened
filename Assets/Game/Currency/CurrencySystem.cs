using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Game.Currency
{
    public class CurrencySystem
    {
        public event Action<CurrencyKind, int> CurrencyChangedEvent;

        private SignalBus _signalBus;
        
        private readonly Dictionary<CurrencyKind, ICurrencyController> _controllers;
        

        public CurrencySystem(SignalBus signalBus, List<ICurrencyController> controllers)
        {
            _signalBus = signalBus;
            _controllers = controllers.ToDictionary(c => c.CurrencyKind);
        }

        public void AddCurrency(CurrencyKind kind, int amount, CurrencyGainPlacementAlias gainPlacementAlias)
        {
            if (amount <= 0)
            {
                SubtractCurrency(kind, -amount);
                return;
            }

            if (_controllers.TryGetValue(kind, out var controller) == false)
            {
                Debug.LogError($"No currency controller found for kind {kind}");
                return;
            }
            
            controller.AddCurrency(amount, gainPlacementAlias);
            var currentAmount = controller.GetCurrencyValue();
            
            CurrencyChangedEvent?.Invoke(kind, currentAmount);
            
            _signalBus.Fire(new Signals.CurrencyAdded
            {
                CurrencyKind = kind,
                CurrentAmount = currentAmount,
                Delta = amount,
                GainPlacementAlias = gainPlacementAlias
            });
        }

        public void SubtractCurrency(CurrencyKind kind, int amount)
        {
            if (_controllers.TryGetValue(kind, out var controller) == false)
            {
                Debug.LogError($"No currency controller found for kind {kind}");
                return;
            }
            
            if (IsEnoughCurrency(kind, amount) == false)
            {
                _signalBus.Fire(new Signals.NotEnoughCurrency
                {
                    CurrencyKind = kind,
                });
                return;
            }
            
            controller.SubtractCurrency(amount);
            var currentAmount = controller.GetCurrencyValue();
            
            CurrencyChangedEvent?.Invoke(kind, currentAmount);
                
            _signalBus.Fire(new Signals.CurrencySubtracted
            {
                CurrencyKind = kind,
                CurrentAmount = currentAmount,
                Delta = amount
            });
        }

        public int GetCurrencyValue(CurrencyKind kind)
        {
            if (_controllers.TryGetValue(kind, out var controller))
            {
                return controller.GetCurrencyValue();
            }
            return 0;
        }

        public bool IsEnoughCurrency(CurrencyKind kind, int amount)
        {
            if (_controllers.TryGetValue(kind, out var controller))
            {
                return controller.IsEnoughCurrency(amount);
            }
            return false;
        }
    }
}
