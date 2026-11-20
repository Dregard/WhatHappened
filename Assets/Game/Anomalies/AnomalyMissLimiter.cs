using System.Collections.Generic;

namespace Game.Anomalies
{
    /// <summary>Counts picture misses in a sliding window and reports when rapid random tapping must be locked out. Pure time-driven logic: callers supply the clock.</summary>
    public sealed class AnomalyMissLimiter
    {
        private readonly int _missLimit;
        private readonly float _window;
        private readonly float _lockDuration;
        private readonly Queue<float> _misses = new();
        private float _lockedUntil = float.NegativeInfinity;

        public AnomalyMissLimiter(int missLimit, float window, float lockDuration)
        {
            _missLimit = missLimit;
            _window = window;
            _lockDuration = lockDuration;
        }

        /// <summary>True while picture taps are rejected at the supplied time.</summary>
        public bool IsLocked(float now)
        {
            return now < _lockedUntil;
        }

        /// <summary>Registers a picture miss; returns true when this miss starts the lock. Misses during a lock are ignored and never extend it. Expired misses leave the counter by time only.</summary>
        public bool RegisterMiss(float now)
        {
            if (IsLocked(now))
                return false;
            while (_misses.Count > 0 && now - _misses.Peek() > _window)
                _misses.Dequeue();
            _misses.Enqueue(now);
            if (_misses.Count < _missLimit)
                return false;
            _misses.Clear();
            _lockedUntil = now + _lockDuration;
            return true;
        }

        /// <summary>Clears the counter and any lock (lock expiry, restart).</summary>
        public void Reset()
        {
            _misses.Clear();
            _lockedUntil = float.NegativeInfinity;
        }
    }
}
