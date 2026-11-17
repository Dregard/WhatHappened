using Game.Profile;
using Zenject;

namespace Game.Currency
{
    public class GoldCurrencyController : ProfileHolderBase<CurrencyProfile>, ICurrencyController
    {
        private GoldCurrencyData _data;
        
        
        public CurrencyKind CurrencyKind => CurrencyKind.Gold;

        public GoldCurrencyController(SignalBus signalBus, GoldCurrencyData data) : base(signalBus, "GoldCurrency")
        {
            _data = data;
        }

        protected override CurrencyProfile CreateDefaultProfile()
        {
            return new CurrencyProfile
            {
                CurrentAmount = _data.StartValue,
            };
        }

        protected override void OnProfileSet()
        {
        }

        public void AddCurrency(int amount, CurrencyGainPlacementAlias gainPlacementAlias)
        {
            if (amount <= 0) return;

            Profile.CurrentAmount += amount;
        }

        public void SubtractCurrency(int amount)
        {
            if (amount <= 0) return;

            if (Profile.CurrentAmount >= amount)
            {
                Profile.CurrentAmount -= amount;

                SignalBus.Fire(new Signals.CurrencySubtracted
                {
                    CurrencyKind = CurrencyKind,
                    CurrentAmount = Profile.CurrentAmount,
                    Delta = amount
                });
            }
            else
            {
                SignalBus.Fire(new Signals.NotEnoughCurrency
                {
                    CurrencyKind = CurrencyKind
                });
            }
        }

        public void SetCurrency(int amount)
        {
            var oldAmount = Profile.CurrentAmount;
            Profile.CurrentAmount = amount;

            if (amount > oldAmount)
            {
                SignalBus.Fire(new Signals.CurrencyAdded
                {
                    CurrencyKind = CurrencyKind,
                    CurrentAmount = Profile.CurrentAmount,
                    Delta = amount - oldAmount,
                    GainPlacementAlias = CurrencyGainPlacementAlias.None
                });
            }
            else if (amount < oldAmount)
            {
                SignalBus.Fire(new Signals.CurrencySubtracted
                {
                    CurrencyKind = CurrencyKind,
                    CurrentAmount = Profile.CurrentAmount,
                    Delta = oldAmount - amount
                });
            }
        }

        public int GetCurrencyValue()
        {
            return Profile?.CurrentAmount ?? 0;
        }

        public bool IsEnoughCurrency(int amount)
        {
            return (Profile?.CurrentAmount ?? 0) >= amount;
        }
    }
}
