using Game.Currency;
using UnityEngine;

namespace Game.Purchase
{
    [CreateAssetMenu(fileName = "CurrencyRewardData", menuName = "Game/Reward/Currency")]
    public class CurrencyRewardData : RewardData
    {
        [SerializeField] private CurrencyKind _currencyKind;
        [SerializeField] private int _amount;

        public CurrencyKind CurrencyKind => _currencyKind;
        public int Amount
        {
            get => _amount;
            set => _amount = value;
        }
    }
}
