using System;
using System.Collections.Generic;
using Game.Analytics.Data;

namespace Game.Analytics
{
    public class LionSdkController : AnalyticsController<LionSdkData>
    {
        private Dictionary<string, Action<Dictionary<string, object>>> _eventHandlersByName = new();


        public override AnalyticsAlias Alias => AnalyticsAlias.LionSdk;

        public override void Initialize(LionSdkData lionSdkData)
        {
            //_eventHandlersByName.Add("MissionStarted", FireMissionStarted);
            //_eventHandlersByName.Add("MissionCompleted", FireMissionCompleted);
            //_eventHandlersByName.Add("MissionAbandoned", FireMissionAbandoned);
            //_eventHandlersByName.Add("MissionFailed", FireMissionFailed);
            //_eventHandlersByName.Add("MissionStep", FireMissionStep);
            //_eventHandlersByName.Add("PurchaseSucceed", FirePurchaseSucceed);
            //_eventHandlersByName.Add("PowerUpUsed", FirePowerUpUsed);
        }

        public override void UpdateParameters(Dictionary<string, object> parameters)
        {
            
        }

        public override void FireEvent(string eventName, Dictionary<string, object> eventData = null)
        {
            
        }

        //     public override void UpdateParameters(Dictionary<string, object> parameters)
    //     {
    //     }
    //
    //     public override void FireEvent(string eventName, Dictionary<string, object> eventData = null)
    //     {
    //         if (_eventHandlersByName.TryGetValue(eventName, out var handler))
    //             handler(eventData);
    //     }
    //
    //     private void FireMissionStarted(Dictionary<string, object> eventData)
    //     {
    //         var sdkData = new MissionEventArgs
    //         {
    //             MissionID = (int)eventData["mission_id"],
    //             MissionName = (string)eventData["mission_name"],
    //             MissionType = (string)eventData["mission_type"],
    //             MissionAttempt = (int)eventData["mission_attempt"],
    //             IsGamePlay = (bool)eventData["is_gameplay"],
    //         };
    //
    //         LionAnalytics.MissionStarted(sdkData);
    //     }
    //
    //     private void FireMissionCompleted(Dictionary<string, object> eventData)
    //     {
    //         var reward = ExtractReward(eventData);
    //
    //         var sdkData = new MissionCompletedEventArgs
    //         {
    //             MissionID = (int)eventData["mission_id"],
    //             MissionName = (string)eventData["mission_name"],
    //             MissionType = (string)eventData["mission_type"],
    //             MissionAttempt = (int)eventData["mission_attempt"],
    //             IsGamePlay = (bool)eventData["is_gameplay"],
    //             Reward = reward,
    //         };
    //
    //         LionAnalytics.MissionCompleted(sdkData);
    //     }
    //
    //     private void FireMissionFailed(Dictionary<string, object> eventData)
    //     {
    //         var additionalData = ExtractAdditionalData(eventData);
    //         var sdkData = new MissionFailedEventArgs(new MissionEventArgs
    //         {
    //             MissionID = (int)eventData["mission_id"],
    //             MissionName = (string)eventData["mission_name"],
    //             MissionType = (string)eventData["mission_type"],
    //             MissionAttempt = (int)eventData["mission_attempt"],
    //             IsGamePlay = (bool)eventData["is_gameplay"],
    //         })
    //         {
    //             FailReason = (string)eventData["fail_reason"]
    //         };
    //
    //         LionAnalytics.MissionFailed(sdkData, additionalData);
    //     }
    //
    //     private void FireMissionStep(Dictionary<string, object> eventData)
    //     {
    //         var additionalData = ExtractAdditionalData(eventData);
    //         var reward = eventData.ContainsKey("reward") ? ExtractReward(eventData) : null;
    //         
    //         var sdkData = new MissionStepEventArgs
    //         {
    //             MissionID = (int)eventData["mission_id"],
    //             MissionName = (string)eventData["mission_name"],
    //             MissionType = (string)eventData["mission_type"],
    //             MissionAttempt = (int)eventData["mission_attempt"],
    //             IsGamePlay = (bool)eventData["is_gameplay"],
    //             StepName = (string)eventData["step_name"],
    //             Reward = reward
    //         };
    //
    //         LionAnalytics.MissionStep(sdkData, additionalData);
    //     }
    //
    //     private void FireMissionAbandoned(Dictionary<string, object> eventData)
    //     {
    //         var additionalData = ExtractAdditionalData(eventData);
    //         var sdkData = new MissionEventArgs
    //         {
    //             MissionID = (int)eventData["mission_id"],
    //             MissionName = (string)eventData["mission_name"],
    //             MissionType = (string)eventData["mission_type"],
    //             MissionAttempt = (int)eventData["mission_attempt"],
    //             IsGamePlay = (bool)eventData["is_gameplay"],
    //         };
    //
    //         LionAnalytics.MissionAbandoned(sdkData, additionalData);
    //     }
    //
    //     private void FirePurchaseSucceed(Dictionary<string, object> eventData)
    //     {
    //         var additionalData = ExtractAdditionalData(eventData);
    //         var transactionName = (string)eventData["transaction_name"];
    //         var price = (PriceData)eventData["price"];
    //         var reward = (RewardData)eventData["reward"];
    //         var priceProduct = MakeProduct(new List<PriceData> { price });
    //         var rewardProduct = MakeProduct(new List<RewardData> { reward });
    //         var placement = (string)eventData["placement"];
    //         
    //         LionAnalytics.Economy(transactionName, priceProduct, rewardProduct, placement, additionalData);
    //     }
    //
    //     private void FirePowerUpUsed(Dictionary<string, object> eventData)
    //     {
    //         var additionalData = ExtractAdditionalData(eventData);
    //         var sdkData = new PowerUpUsedEventArgs
    //         {
    //             MissionAttempt = (int)eventData["mission_attempt"],
    //             MissionID = (string)eventData["mission_id"],
    //             MissionName = (string)eventData["mission_name"],
    //             MissionType = (string)eventData["mission_type"],
    //             PowerUpName = (string)eventData["power_up_name"],
    //         };
    //         
    //         LionAnalytics.PowerUpUsed(sdkData, additionalData);
    //     }
    //
    //     private Reward ExtractReward(Dictionary<string, object> eventData)
    //     {
    //         var rewards = (List<RewardData>)eventData["reward"];
    //         var product = MakeProduct(rewards);
    //         var reward = new Reward(product);
    //         
    //         return reward;
    //     }
    //
    //     private Product MakeProduct(List<RewardData> rewards)
    //     {
    //         var virtualCurrencies = new List<VirtualCurrency>();
    //         var items = new List<Item>();
    //         foreach (var rewardData in rewards)
    //         {
    //             switch (rewardData)
    //             {
    //                 case CurrencyRewardData currencyRewardData:
    //                 {
    //                     virtualCurrencies.Add(new VirtualCurrency(currencyRewardData.CurrencyKind.ToString(),
    //                         "soft", currencyRewardData.Amount));
    //                     break;
    //                 }
    //                 case BoosterRewardData boosterRewardData:
    //                 {
    //                     items.Add(new Item(boosterRewardData.BoosterKind.ToString(),
    //                         "booster", boosterRewardData.Amount));
    //                     break;
    //                 }
    //             }
    //         }
    //
    //         var product = new Product
    //         {
    //             virtualCurrencies = virtualCurrencies,
    //             items = items,
    //         };
    //         return product;
    //     }
    //     
    //     private Product MakeProduct(List<PriceData> prices)
    //     {
    //         var virtualCurrencies = new List<VirtualCurrency>();
    //         foreach (var priceData in prices)
    //         {
    //             switch (priceData)
    //             {
    //                 case CurrencyPriceData currencyPriceData:
    //                 {
    //                     virtualCurrencies.Add(new VirtualCurrency(currencyPriceData.CurrencyKind.ToString(),
    //                         "soft", currencyPriceData.Amount));
    //                     break;
    //                 }
    //             }
    //         }
    //         
    //         var product = new Product
    //         {
    //             virtualCurrencies = virtualCurrencies,
    //         };
    //         return product;
    //     }
    //     
    //     private Dictionary<string, object> ExtractAdditionalData(Dictionary<string, object> eventData)
    //     {
    //         var additionalData = (Dictionary<string, Dictionary<string, object>>)eventData["additional_data"];
    //         var result = additionalData.ToDictionary(
    //             pair => pair.Key, 
    //             pair => (object)pair.Value);
    //         
    //         return result;
    //     }
    }
}