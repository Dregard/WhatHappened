using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    public class PauseService
    {
        private readonly IEnumerable<IPausable> _pausables;
        private readonly HashSet<object> _pauseRequesters = new();

        public PauseService(IEnumerable<IPausable> pausables)
        {
            _pausables = pausables;
        }

        public void Pause(object requester)
        {
            if (_pauseRequesters.Count == 0)
            {
                foreach (var pausable in _pausables)
                {
                    pausable.Pause();
                }
            }
            
            _pauseRequesters.Add(requester);
            
            Debug.Log($"[PauseService] Pause requested by {requester.GetType().Name}. Total requesters: {_pauseRequesters.Count}");
        }

        public void Unpause(object requester)
        {
            if (!_pauseRequesters.Contains(requester))
            {
                // Debug.LogWarning($"[PauseService] Unpause requested by {requester.GetType().Name}, but it never requested a pause.");
                return;
            }

            _pauseRequesters.Remove(requester);

            if (_pauseRequesters.Count == 0)
            {
                foreach (var pausable in _pausables)
                {
                    pausable.Unpause();
                }
            }
            
            Debug.Log($"[PauseService] Unpause requested by {requester.GetType().Name}. Total requesters: {_pauseRequesters.Count}");
        }

        public void ForceUnpauseAll()
        {
            _pauseRequesters.Clear();
            foreach (var pausable in _pausables)
            {
                pausable.Unpause();
            }
            Debug.Log("[PauseService] Force unpaused all.");
        }
    }
}
