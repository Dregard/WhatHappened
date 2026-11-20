using Game.UI.Windows.General;

namespace Game.Gameplay.Chapters
{
    /// <summary>Carries the chapter owner's snapshot for one chapter window; creation never grants rewards or changes progression.</summary>
    public sealed class ChapterWindowModelParams<TModel> : ModelParams
        where TModel : class, IWindowModel
    {
        public TModel Model;
    }
}
