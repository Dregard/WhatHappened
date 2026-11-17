using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Effects
{
    [Serializable]
    public struct EffectPoolSettings
    {
        public Effect Prefab;
        public int InitialCount;
    }

    [CreateAssetMenu(fileName = "EffectsData", menuName = "Game/Effects")]
    public class EffectsData : ScriptableObject
    {
        [SerializeField] private List<EffectPoolSettings> _effectPoolSettings;

        public IReadOnlyList<EffectPoolSettings> EffectPoolSettings => _effectPoolSettings;
    }
}
