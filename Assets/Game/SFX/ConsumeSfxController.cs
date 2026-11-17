using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Game.SFX
{
    public class ConsumeSfxController : ITickable
    {
        private const string ConsumeSfxId = "Consume";
        private const string TargetConsumeSfxId = "TargetConsume";
        private const string FlySfxId = "TargetFly";
        private const float PlayInterval = 0.03f;
        private const int MaxCount = 5;

        private readonly AudioSystem _audioSystem;
        private readonly Queue<bool> _playQueue = new();
        private float _lastPlayTime;

        
        public ConsumeSfxController(AudioSystem audioSystem)
        {
            _audioSystem = audioSystem;
        }

        public void PlayConsumeSfx(bool isTargetItem)
        {
            if (_playQueue.Count >= MaxCount)
                return;

            _playQueue.Enqueue(isTargetItem);
        }

        public void Tick()
        {
            if (_playQueue.Count == 0)
                return;

            if (Time.time - _lastPlayTime < PlayInterval)
                return;

            var isTargetItem = _playQueue.Dequeue();
            Play(isTargetItem);
            _lastPlayTime = Time.time;
        }

        private void Play(bool isTargetItem)
        {
            if (isTargetItem)
            {
                _audioSystem.PlaySfx(TargetConsumeSfxId);
                _audioSystem.PlaySfx(FlySfxId);
            }
            else
            {
                _audioSystem.PlaySfx(ConsumeSfxId);
            }
        }
    }
}
