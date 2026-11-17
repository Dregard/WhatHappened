using System.Linq;
using UnityEngine;

namespace Game.SFX
{
    [CreateAssetMenu(fileName = nameof(SfxData), menuName = "Game/SFX/Config")]
    public class SfxData : ScriptableObject
    {
        [SerializeField] private SfxEmitter[] _prefabs;

        
        public SfxEmitter GetPrefab(string id)
        {
            return _prefabs.First(x => x.Id == id);
        }
        
        public string GetName<T>() where T : SfxEmitter
        {
            return _prefabs.First(x => x is T).name;
        }
    }
}