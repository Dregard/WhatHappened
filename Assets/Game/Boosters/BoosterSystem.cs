using System;
using System.Collections.Generic;
using System.Linq;
using Game.Gameplay;
using Game.Levels;
using Game.UI.Windows.BoosterUnlockWindow;
using Game.UI.Windows.General;
using Zenject;

namespace Game.Boosters
{
    public class BoosterSystem
    {
        private readonly BoostersProfileController _profileController;
        private readonly Dictionary<BoosterKind, Booster> _boostersByKind;
        private readonly WindowSystem _windowSystem;
        private readonly SignalBus _signalBus;
        private readonly GameplayProgressController _gameplayProgressController;
        private readonly LevelProgressController _levelProgressController;

        public IReadOnlyList<Booster> Boosters => _boostersByKind.Values.ToList();

        public event Action<BoosterKind, int> OnBoosterCountChanged;


        public BoosterSystem(BoostersProfileController profileController, List<Booster> boosters,
            WindowSystem windowSystem, SignalBus signalBus, GameplayProgressController gameplayProgressController,
            LevelProgressController levelProgressController)
        {
            _profileController = profileController;
            _boostersByKind = boosters.ToDictionary(b => b.Kind, b => b);
            _windowSystem = windowSystem;
            _signalBus = signalBus;
            _gameplayProgressController = gameplayProgressController;
            _levelProgressController = levelProgressController;
        }

        public int GetCount(BoosterKind kind)
        {
            return _profileController.GetBoosterCount(kind);
        }

        public void Add(BoosterKind kind, int count)
        {
            _profileController.AddBooster(kind, count);
            OnBoosterCountChanged?.Invoke(kind, GetCount(kind));
        }

        /// <summary>Resets every unlocked per-level booster (the photo) to its configured level allowance without accumulating leftovers.</summary>
        public void RefillPerLevelBoosters()
        {
            foreach (var booster in _boostersByKind.Values)
            {
                if (booster.PerLevelCount <= 0 || !IsOpened(booster.Kind))
                    continue;
                _profileController.SetBoosterCount(booster.Kind, booster.PerLevelCount);
                OnBoosterCountChanged?.Invoke(booster.Kind, GetCount(booster.Kind));
            }
        }

        /// <summary>Consumes inventory only for an unlocked, currently applicable effect; invalid and repeated actions preserve stock.</summary>
        public bool TryUse(BoosterKind kind)
        {
            if (_boostersByKind.TryGetValue(kind, out var booster) == false)
                return false;

            if (!IsOpened(kind) || !booster.CanApply())
                return false;

            if (_profileController.UseBooster(kind))
            {
                booster.Apply();
                OnBoosterCountChanged?.Invoke(kind, GetCount(kind));

                _signalBus.Fire(new Signals.BoosterUsed
                {
                    BoosterKind = kind,
                    LevelNumber = _gameplayProgressController.CurrentLevel,
                    LevelAttempt = _gameplayProgressController.CurrentAttempt,
                    LevelProgress = _levelProgressController.GetLevelProgress(),
                });

                return true;
            }

            return false;
        }

        public BoosterViewModel GetViewModel(BoosterKind boosterKind)
        {
            var booster = _boostersByKind[boosterKind];
            return booster.GetViewModel();
        }

        public bool IsOpened(BoosterKind kind)
        {
            return _profileController.IsOpened(kind);
        }

        public bool CanUse(BoosterKind kind)
        {
            if (_boostersByKind.TryGetValue(kind, out var booster))
            {
                return booster.CanApply();
            }

            return false;
        }

        public void CheckUnlocks(int level)
        {
            foreach (var booster in _boostersByKind.Values)
            {
                if (booster.OpenLevel <= level && !IsOpened(booster.Kind))
                {
                    _profileController.OpenBooster(booster.Kind);
                    Add(booster.Kind, booster.InitialValue);
                    OpenUnlockWindow(booster.Kind);
                }
            }
        }

        private void OpenUnlockWindow(BoosterKind kind)
        {
            _windowSystem.AddToQueue<BoosterUnlockWindow>(new BoosterUnlockWindowModelParams { BoosterKind = kind });
        }
    }
}
