using Game.UI.Windows.General;

namespace Game.Gameplay.Chapters
{
    public sealed class ChapterWindowModelProcessor : WindowModelProcessorBase<ChapterWindowModel, ChapterWindowModelParams>
    {
        private ChapterWindowModel _model;

        /// <summary>Receives the chapter owner's snapshot; window creation never grants rewards or changes progression.</summary>
        public override void SetParams(ChapterWindowModelParams modelParams)
        {
            _model = modelParams.Model;
        }

        /// <summary>Returns the supplied passive snapshot for investigation or chapter victory.</summary>
        public override ChapterWindowModel GetModel()
        {
            var model = _model;
            _model = null;
            return model;
        }
    }
}
