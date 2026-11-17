
namespace Game.Boosters
{
    public abstract class Booster
    {
        public abstract BoosterKind Kind { get; }
        public virtual int OpenLevel => 0;

        public abstract int InitialValue { get; }
        /// <summary>Stock this booster is reset to at the start of every level; zero keeps ordinary accumulating stock.</summary>
        public virtual int PerLevelCount => 0;
        public abstract void Apply();
        public virtual bool CanApply() => true;
        public abstract BoosterViewModel GetViewModel();
    }
}
