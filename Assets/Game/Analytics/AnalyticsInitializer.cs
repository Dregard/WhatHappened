using System.Collections.Generic;
using System.Linq;
using Game.Analytics.Data;
using Zenject;

namespace Game.Analytics
{
    public class AnalyticsInitializer
    {
        private readonly AnalyticsData _analyticsData;
        private readonly SignalBus _signalBus;
        
        private List<IAnalyticsController> _controllers;


        public AnalyticsInitializer(AnalyticsData analyticsData, IEnumerable<IAnalyticsController> controllers, SignalBus signalBus)
        {
            _analyticsData = analyticsData;
            _controllers = new List<IAnalyticsController>(controllers);
            _signalBus = signalBus;
        }
        
        public void Initialize()
        {
            var parametersFormer = new AnalyticsParametersFormer();
            var observer = new AnalyticsObserver(parametersFormer, _signalBus);
            
            var controllersToKeep = new List<IAnalyticsController>();
            var providerDatas = _analyticsData.ProviderDatas;
            foreach (var providerData in providerDatas)
            {
                var controller = _controllers.FirstOrDefault(x => x.Alias == providerData.Alias);
                if (controller == null)
                {
                    continue;
                }
                
                controller.Initialize(providerData);
                controllersToKeep.Add(controller);
                observer.AddController(controller);
            }
            _controllers = controllersToKeep;
            
            observer.UpdateParameters();
            observer.StartObservation();
        }
    }
}