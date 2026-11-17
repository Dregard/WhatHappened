using Game.Currency;
using UnityEngine;

namespace Game.Purchase
{
    [CreateAssetMenu(fileName = "CurrencyPriceData", menuName = "Game/Price/Currency")]
    public class CurrencyPriceData : PriceData
    {
        [SerializeField] private CurrencyKind _currencyKind;
        [SerializeField] private int _amount;

        public CurrencyKind CurrencyKind => _currencyKind;
        public int Amount => _amount;
    }
}
