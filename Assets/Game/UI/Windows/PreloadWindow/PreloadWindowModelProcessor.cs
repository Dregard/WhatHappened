using Game.UI.Windows.General;

namespace Game.UI.Windows.PreloadWindow
{
    public class PreloadWindowModelProcessor : WindowModelProcessorBase<PreloadWindowModel, PreloadWindowModel>
    {
        private PreloadWindowModel _model;

        /// <summary>Receives the state owned by this preparation, before the window is shown.</summary>
        public override void SetParams(PreloadWindowModel modelParams)
        {
            _model = modelParams;
        }

        /// <summary>Consumes the supplied state once so another opening starts from zero.</summary>
        public override PreloadWindowModel GetModel()
        {
            var model = _model ?? new PreloadWindowModel();
            _model = null;
            return model;
        }
    }
}
