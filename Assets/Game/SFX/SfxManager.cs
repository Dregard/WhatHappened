using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Game.SFX
{
    public class SfxManager : MonoBehaviour
    {
        private SfxData _data;
        private readonly Dictionary<string, List<SfxEmitter>> _controllerListByName = new();
        
        
        [Inject]
        public void Construct(SfxData data)
        {
            _data = data;
        }
        
        public void Play(string sfxName, float volume, float delay = 0f)
        {
            var controller = GetFirstReady(sfxName);
            controller.Play(delay, volume);
        }
        
        public void Stop(string sfxName)
        {
            _controllerListByName[sfxName].ForEach(x => x.Stop());
        }
        
        private SfxEmitter GetFirstReady(string sfxName)
        {
            var list = GetList(sfxName);
            var item = list.FirstOrDefault(x => x.IsPlaying == false);
            if (item == null)
            {
                var proto = _data.GetPrefab(sfxName);
                item = Instantiate(proto, transform);
                list.Add(item);
            }

            return item;
        }
        
        private List<SfxEmitter> GetList(string sfxName)
        {
            if (_controllerListByName.TryGetValue(sfxName, out var list) == false)
            {
                list = new List<SfxEmitter>();
                _controllerListByName[sfxName] = list;
            }

            return list;
        }

        public void SetVolume(float value)
        {
            foreach (var controllers in _controllerListByName.Values)
            {
                controllers.ForEach(x => x.SetVolume(value));
            }
        }
    }
}