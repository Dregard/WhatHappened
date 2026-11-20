using Game.UI.Windows.General;

namespace Game.Gameplay.Chapters
{
    /// <summary>Passes a supplied passive snapshot to its chapter window; one processor type serves each chapter window model.</summary>
    public sealed class ChapterWindowModelProcessor<TModel> : WindowModelProcessorBase<TModel, ChapterWindowModelParams<TModel>>
        where TModel : class, IWindowModel
    {
        private TModel _model;

        public override void SetParams(ChapterWindowModelParams<TModel> modelParams)
        {
            _model = modelParams.Model;
        }

        public override TModel GetModel()
        {
            var model = _model;
            _model = null;
            return model;
        }
    }
}
