using Game.Anomalies;
using Game.Gameplay;
using Game.SFX;

namespace Game.Boosters
{
    // Retains the serialized Compass inventory/unlock identity for the anomaly hint.
    public class CompassBooster : Booster
    {
        private readonly LevelsManager _levelsManager;
        private readonly CompassBoosterData _data;
        private readonly AudioSystem _audioSystem;

        /// <summary>Adapts the existing hint stock, unlocks and purchases to the current anomaly attempt.</summary>
        public CompassBooster(LevelsManager levelsManager, CompassBoosterData data, AudioSystem audioSystem)
        {
            _levelsManager = levelsManager;
            _data = data;
            _audioSystem = audioSystem;
        }

        public override BoosterKind Kind { get { return BoosterKind.Compass; } }
        public override int OpenLevel { get { return _data.OpenLevel; } }
        public override int InitialValue { get { return _data.InitialValue; } }

        /// <summary>Allows spending only for an active unfound target and valid authored finger presentation.</summary>
        public override bool CanApply()
        {
            return _levelsManager.CurrentLevelController?.AnomalyLevel?.Boosters.CanHint == true &&
                _data.HintDuration > 0 && float.IsFinite(_data.HintDuration) && AnomalyBoosterController.IsValidHint(_data.Hint);
        }

        /// <summary>Shows one animated finger; an accepted finding clears it immediately without revealing another target for free.</summary>
        public override void Apply()
        {
            if (CanApply() && _levelsManager.CurrentLevelController.AnomalyLevel.Boosters.TryHint(_data.Hint, _data.HintDuration))
                _audioSystem?.PlaySfx("Compass");
        }

        /// <summary>Returns the existing authored purchase reward, price and hint presentation.</summary>
        public override BoosterViewModel GetViewModel()
        {
            return new BoosterViewModel
            {
                Description = _data.Description, Icon = _data.Icon, Name = _data.Name,
                Price = _data.Price, Reward = _data.Reward,
            };
        }
    }
}
