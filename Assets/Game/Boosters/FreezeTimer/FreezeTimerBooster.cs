using Game.Gameplay;
using Game.SFX;

namespace Game.Boosters
{
    // Retains the serialized FreezeTimer inventory/unlock identity for the comparison photo.
    public class FreezeTimerBooster : Booster
    {
        private readonly LevelsManager _levelsManager;
        private readonly FreezeTimerBoosterData _data;
        private readonly AudioSystem _audioSystem;

        /// <summary>Adapts the existing stock, unlocks and purchases to the current attempt's photo allowance.</summary>
        public FreezeTimerBooster(LevelsManager levelsManager, FreezeTimerBoosterData data, AudioSystem audioSystem)
        {
            _levelsManager = levelsManager;
            _data = data;
            _audioSystem = audioSystem;
        }

        public override BoosterKind Kind { get { return BoosterKind.FreezeTimer; } }
        public override int OpenLevel { get { return _data.OpenLevel; } }
        public override int InitialValue { get { return _data.InitialValue; } }
        public override int PerLevelCount { get { return _data.PhotosPerLevel; } }

        /// <summary>Rejects preview, pause, duplicate presses, completed stages and the fourth photo of an attempt.</summary>
        public override bool CanApply()
        {
            return _levelsManager.CurrentLevelController?.AnomalyLevel?.Boosters.CanPhoto == true;
        }

        /// <summary>Opens one five-second original; the level clock observes this effect without changing external pause/freeze flags.</summary>
        public override void Apply()
        {
            if (CanApply() && _levelsManager.CurrentLevelController.AnomalyLevel.Boosters.TryPhoto())
                _audioSystem?.PlaySfx("GlassSnap");
        }

        /// <summary>Returns the existing authored photo purchase reward and price.</summary>
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
