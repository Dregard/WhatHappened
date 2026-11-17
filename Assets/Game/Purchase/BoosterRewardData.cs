using Game.Boosters;
using UnityEngine;

namespace Game.Purchase
{
    [CreateAssetMenu(fileName = "BoosterRewardData", menuName = "Game/Reward/Booster")]
    public class BoosterRewardData : RewardData
    {
        [SerializeField] private BoosterKind _currencyKind;
        [SerializeField] private int _amount;
        
        public BoosterKind BoosterKind => _currencyKind;
        public int Amount => _amount;
    }
}