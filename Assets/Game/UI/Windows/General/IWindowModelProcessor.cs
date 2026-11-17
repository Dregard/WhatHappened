using System;

namespace Game.UI.Windows.General
{
    public interface IWindowModelProcessor<out TModel, in TParams> : IWindowModelProcessor
        where TModel : class, IWindowModel
        where TParams : ModelParams 
    {
        public new TModel GetModel();
        public void SetParams(TParams modelParams);
    }
    
    public interface IWindowModelProcessor
    {
        public Type GetModelType();
        public IWindowModel GetModel();
        public void SetParams(ModelParams modelParams);
    }
}