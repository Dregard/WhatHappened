using UnityEngine;
using System.Collections.Generic;
using System;
using Zenject;

namespace Game.Effects
{
    public class EffectSystem : IInitializable
    {
        private readonly EffectsData _settings;
        private readonly DiContainer _container;
        
        private readonly Dictionary<Type, Queue<Effect>> _pools = new Dictionary<Type, Queue<Effect>>();
        private readonly Dictionary<Type, Effect> _prefabsByType = new Dictionary<Type, Effect>();
        private Transform _poolRoot;

        
        public EffectSystem(EffectsData settings, DiContainer container)
        {
            _settings = settings;
            _container = container;
        }

        public void Initialize()
        {
            _poolRoot = new GameObject("EffectPool").transform;
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var setting in _settings.EffectPoolSettings)
            {
                if (setting.Prefab == null) continue;
                
                var type = setting.Prefab.GetType();
                if (!_pools.ContainsKey(type))
                {
                    _pools[type] = new Queue<Effect>();
                    _prefabsByType[type] = setting.Prefab;

                    for (var i = 0; i < setting.InitialCount; i++)
                    {
                        var effect = _container.InstantiatePrefabForComponent<Effect>(setting.Prefab, _poolRoot);
                        effect.gameObject.SetActive(false);
                        effect.transform.SetParent(_poolRoot, false);
                        _pools[type].Enqueue(effect);
                    }
                }
            }
        }

        public T Play<T>(Vector3 position, Transform parent = null) where T : Effect
        {
            var type = typeof(T);
            if (!_pools.ContainsKey(type))
            {
                Debug.LogError($"Effect of type {type} is not registered in EffectSystem");
                return null;
            }

            T effect;
            if (_pools[type].Count > 0)
            {
                effect = (T)_pools[type].Dequeue();
            }
            else
            {
                effect = _container.InstantiatePrefabForComponent<T>(_prefabsByType[type], _poolRoot);
            }

            effect.transform.SetParent(parent, false);
            effect.transform.position = position;
            effect.gameObject.SetActive(true);
            effect.Finished += HandleEffectFinished;
            effect.Play();

            return effect;
        }

        public void Stop(Effect effect)
        {
            if (effect == null) return;
            
            effect.Stop();
        }

        private void HandleEffectFinished(Effect effect)
        {
            effect.Finished -= HandleEffectFinished;
            effect.ResetEffect();
            effect.gameObject.SetActive(false);
            effect.transform.SetParent(_poolRoot, false);
            
            var type = effect.GetType();
            _pools[type].Enqueue(effect);
        }
    }
}
