using System;

namespace Game.UI.Windows.General
{
    public abstract class WindowModelProcessorBase<TModel, TParams> : IWindowModelProcessor<TModel, TParams>
        where TModel : class, IWindowModel
        where TParams : ModelParams
    {
        public Type GetModelType()
        {
            return typeof(TModel);
        }

        IWindowModel IWindowModelProcessor.GetModel()
        {
            return GetModel();
        }

        public virtual void SetParams(TParams modelParams)
        {
        }

        public void SetParams(ModelParams modelParams)
        {
            if (modelParams is TParams typedParams)
            {
                SetParams(typedParams);
            }
        }


        public abstract TModel GetModel();
    }
}