using Game.Purchase;
using UnityEngine;

namespace Game.Boosters
{
    public class BoosterData : ScriptableObject
    {
        public string Name;
        public string Description;
        public Sprite Icon;
        public int InitialValue;
        public PriceData Price;
        public BoosterRewardData Reward;
    }
}