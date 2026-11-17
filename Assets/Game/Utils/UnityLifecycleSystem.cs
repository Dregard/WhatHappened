using System.Collections.Generic;
using UnityEngine;

namespace Game.Utils
{
    public class UnityLifecycleSystem : MonoBehaviour
    {
        private List<IApplicationPauseListener> _applicationPauseListeners = new List<IApplicationPauseListener>();

        private void OnApplicationPause(bool pauseStatus)
        {
            foreach (var listener in _applicationPauseListeners)
            {
                listener.OnApplicationPause(pauseStatus);
            }
        }
        
        public void AddListener(IApplicationPauseListener listener)
        {
            _applicationPauseListeners.Add(listener);
        }
        
        public void RemoveListener(IApplicationPauseListener listener)
        {
            _applicationPauseListeners.Remove(listener);
        }
    }
}