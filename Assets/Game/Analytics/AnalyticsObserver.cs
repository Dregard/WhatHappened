using System.Collections.Generic;
using Game.Gameplay;
using Game.Purchase;
using UnityEngine;
using Zenject;

namespace Game.Analytics
{
    public class AnalyticsObserver
    {
        private readonly SignalBus _signalBus;

        private readonly List<IAnalyticsController> _controllers = new();
        private readonly AnalyticsParametersFormer _parametersFormer;


        public AnalyticsObserver(AnalyticsParametersFormer parametersFormer, SignalBus signalBus)
        {
            _parametersFormer = parametersFormer;

            _signalBus = signalBus;
        }

        public void StartObservation()
        {
            _signalBus.Subscribe<Signals.LevelStarted>(HandleLevelStarted);
            _signalBus.Subscribe<Signals.LevelFinished>(HandleLevelFinished);
            _signalBus.Subscribe<Signals.LevelFailed>(HandleLevelFailed);
            _signalBus.Subscribe<Signals.LevelFailedSoft>(HandleLevelFailed);
            _signalBus.Subscribe<Signals.LevelAbandoned>(HandleLevelAbandoned);
            _signalBus.Subscribe<Singals.PurchaseSucceed>(HandlePurchaseSucceed);
            _signalBus.Subscribe<Boosters.Signals.BoosterUsed>(PowerUpUsed);
            _signalBus.Subscribe<UI.Windows.Signals.ExtraTimeBoughtSignal>(HandleTimeRevive);
            _signalBus.Subscribe<UI.Windows.Signals.ExtraLifeBoughtSignal>(HandleLifeRevive);
            _signalBus.Subscribe<Tutorial.Signals.TutorialStepCompletedSignal>(HandleTutorialStepCompleted);
        }

        private void HandleTutorialStepCompleted(Tutorial.Signals.TutorialStepCompletedSignal signal)
        {
            var missionId = $"main_{signal.LevelNumber}";
            var stepAdditionalData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "gameplay_data", new Dictionary<string, object>
                    {
                        { "mission_progress", signal.LevelProgress },
                        { "soft_fail_reason", "None" },
                    }
                },
            };
            var missionStepData = new Dictionary<string, object>
            {
                { "mission_id", signal.LevelNumber },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", missionId },
                { "additional_data", stepAdditionalData },
                { "step_name", null },
                { "is_gameplay", true },
            };
            
            FireEvent("MissionStep", missionStepData);
        }

        private void HandleLifeRevive(UI.Windows.Signals.ExtraLifeBoughtSignal signal)
        {
            var stepAdditionalData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "gameplay_data", new Dictionary<string, object>
                    {
                        { "mission_progress", signal.LevelProgress },
                    }
                },
            };
            
            var missionId = $"main_{signal.LevelNumber}";
            var missionStepData = new Dictionary<string, object>
            {
                { "mission_id", signal.LevelNumber },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", missionId },
                { "additional_data", stepAdditionalData },
                { "is_gameplay", true },
                { "step_name", "revive" },
            };
            
            FireEvent("MissionStep", missionStepData);
        }

        private void HandleTimeRevive(UI.Windows.Signals.ExtraTimeBoughtSignal signal)
        {
            var stepAdditionalData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "gameplay_data", new Dictionary<string, object>
                    {
                        { "mission_progress", signal.LevelProgress },
                    }
                },
            };
            
            var missionId = $"main_{signal.LevelNumber}";
            var missionStepData = new Dictionary<string, object>
            {
                { "mission_id", signal.LevelNumber },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", missionId },
                { "additional_data", stepAdditionalData },
                { "is_gameplay", true },
                { "step_name", "revive" },
            };
            
            FireEvent("MissionStep", missionStepData);
        }
        
        private void HandleLevelFailed(Signals.LevelFailed signal)
        {
            var missionId = $"main_{signal.LevelNumber}";
            var additionalData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "gameplay_data", new Dictionary<string, object>
                    {
                        { "mission_progress", signal.LevelProgress },
                    }
                },
            };
            var eventData = new Dictionary<string, object>
            {
                { "mission_id", signal.LevelNumber },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", missionId },
                { "additional_data", additionalData },
                { "is_gameplay", true },
                { "fail_reason", signal.Reason.ToString() }
            };

            FireEvent("MissionFailed", eventData);
        }

        private void HandleLevelFailed(Signals.LevelFailedSoft signal)
        {
            var missionId = $"main_{signal.LevelNumber}";
            var stepAdditionalData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "gameplay_data", new Dictionary<string, object>
                    {
                        { "mission_progress", signal.LevelProgress },
                        { "soft_fail_reason", signal.Reason.ToString() }
                    }
                },
            };
            var missionStepData = new Dictionary<string, object>
            {
                { "mission_id", signal.LevelNumber },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", missionId },
                { "additional_data", stepAdditionalData },
                { "step_name", "soft_fail" },
                { "is_gameplay", true },
            };
            
            FireEvent("MissionStep", missionStepData);
        }

        private void HandleLevelFinished(Signals.LevelFinished signal)
        {
            var missionId = $"main_{signal.LevelNumber}";
            var eventData = new Dictionary<string, object>
            {
                { "mission_id", signal.LevelNumber },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", missionId },
                { "additional_data", null },
                { "reward", signal.Rewards },
                { "is_gameplay", true },
                { "fail_reason", "none" }
            };

            FireEvent("MissionCompleted", eventData);
        }

        private void HandleLevelStarted(Signals.LevelStarted signal)
        {
            var missionId = $"main_{signal.LevelNumber}";
            var eventData = new Dictionary<string, object>
            {
                { "mission_id", signal.LevelNumber },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", missionId },
                { "additional_data", null },
                { "is_gameplay", true },
                { "fail_reason", "none" }
            };

            FireEvent("MissionStarted", eventData);
        }

        private void HandleLevelAbandoned(Signals.LevelAbandoned signal)
        {
            var missionId = $"main_{signal.LevelNumber}";
            var additionalData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "gameplay_data", new Dictionary<string, object>
                    {
                        { "mission_progress", signal.LevelProgress },
                    }
                },
            };
            var eventData = new Dictionary<string, object>
            {
                { "mission_id", signal.LevelNumber },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", missionId },
                { "additional_data", additionalData },
                { "is_gameplay", true },
                { "fail_reason", "abandoned" }
            };

            FireEvent("MissionAbandoned", eventData);
        }

        private void HandlePurchaseSucceed(Singals.PurchaseSucceed signal)
        {
            var additionalData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "mission_data", new Dictionary<string, object>
                    {
                        { "mission_type", "main" },
                        { "mission_name", $"main_{signal.LevelNumber}" },
                        { "mission_id", signal.LevelNumber },
                        { "mission_attempt", signal.LevelAttempt }
                    }
                },
                {
                    "gameplay_data", new Dictionary<string, object>
                    {
                        { "mission_progress", signal.LevelProgress },
                    }
                }
            };
            
            var eventData = new Dictionary<string, object>
            {
                { "placement", signal.PlacementAlias.ToString() },
                { "transaction_name", signal.PurchaseId },
                { "price", signal.PriceData },
                { "reward", signal.RewardData },
                { "additional_data", additionalData },
            };
            
            FireEvent("PurchaseSucceed", eventData);
        }

        private void PowerUpUsed(Boosters.Signals.BoosterUsed signal)
        {
            var additionalData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "gameplay_data", new Dictionary<string, object>
                    {
                        { "mission_progress", signal.LevelProgress },
                    }
                }
            };
            
            var eventData = new Dictionary<string, object>
            {
                { "power_up_name", signal.BoosterKind.ToString() },
                { "mission_id", signal.LevelNumber.ToString() },
                { "mission_attempt", signal.LevelAttempt },
                { "mission_type", "main" },
                { "mission_name", $"main_{signal.LevelNumber}" },
                { "additional_data", additionalData },
            };
            
            FireEvent("PowerUpUsed", eventData);
        }

        public void UpdateParameters()
        {
            var parameters = _parametersFormer.FormParameters();
            foreach (var controller in _controllers)
                controller.UpdateParameters(parameters);
        }

        private void FireEvent(string eventName, Dictionary<string, object> eventData = null)
        {
            Debug.Log($"Fire event: {eventName}");

            foreach (var controller in _controllers)
                controller.FireEvent(eventName, eventData);
        }

        public void AddController(IAnalyticsController controller)
        {
            if (_controllers.Contains(controller))
            {
                Debug.LogWarning($"There is already {controller.GetType()} in the list!");
                return;
            }

            _controllers.Add(controller);
        }
    }
}