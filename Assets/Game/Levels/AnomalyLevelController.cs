using System;
using System.Collections.Generic;
using Game.Anomalies;
using UnityEngine;

namespace Game.Levels
{
    public sealed class AnomalyLevelController : IDisposable
    {
        private readonly AnomalySearchView _view;
        private readonly LevelProgressController _progress;
        private readonly Action _start;
        private readonly Action<string> _playSfx;
        private readonly Action<string> _stopSfx;
        private readonly HashSet<string> _sfxIdsSet = new();
        /// <summary>Raised when the player taps the HUD photo to open the comparison view.</summary>
        public event Action PhotoRequested;
        public AnomalySearchSystem Search { get; }
        public AnomalyBoosterController Boosters { get { return _view.Boosters; } }
        public Sprite ClueImage { get; }
        public string ClueName { get; }

        /// <summary>Owns attempt-local search and its view; completion is forwarded only after all presentation gates.</summary>
        public AnomalyLevelController(Canvas canvas, AnomalySearchView viewPrefab, AnomalyLevelData data, LevelProgressController progress, Action start,
            Action<string> playSfx = null, Action<string> stopSfx = null, Action<GameObject> inject = null)
        {
            ClueImage = data.ClueImage;
            ClueName = data.ClueName;
            Search = new AnomalySearchSystem(data, UnityEngine.Random.Range(0, int.MaxValue));
            Search.IsPaused = true;
            _progress = progress;
            _start = start;
            _playSfx = playSfx;
            _stopSfx = stopSfx;
            _progress.Start(1);
            _progress.SetCompletionBlocked(true);
            _view = AnomalySearchView.Create(canvas, viewPrefab, data, Search, inject);
            _view.Ready += Start;
            _view.SfxRequested += PlaySfx;
            _view.PhotoRequested += RequestPhoto;
            Search.Completed += Complete;
            foreach (var warning in Search.Warnings)
                Debug.LogWarning(warning);
        }

        /// <summary>Routes presentation cues through the injected shared audio system and its user volume settings.</summary>
        private void PlaySfx(string id)
        {
            _sfxIdsSet.Add(id);
            _playSfx?.Invoke(id);
        }

        /// <summary>Forwards the HUD photo tap to the economy adapter.</summary>
        private void RequestPhoto()
        {
            PhotoRequested?.Invoke();
        }

        /// <summary>Enables the attempt clock and search together after Ready.</summary>
        private void Start()
        {
            _start();
            Search.Start();
        }

        /// <summary>Releases the shared progress gate after final finding and transition presentation.</summary>
        private void Complete()
        {
            _progress.ItemPlaced();
            _progress.SetCompletionBlocked(false);
        }

        /// <summary>Discards unfinished stage/finding state on unload and removes visual consumers before asset release.</summary>
        public void Dispose()
        {
            Search.Completed -= Complete;
            if (_view != null)
            {
                _view.Ready -= Start;
                _view.SfxRequested -= PlaySfx;
                _view.PhotoRequested -= RequestPhoto;
                _view.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_view.gameObject);
            }
            foreach (var id in _sfxIdsSet)
                _stopSfx?.Invoke(id);
            _sfxIdsSet.Clear();
        }
    }
}
