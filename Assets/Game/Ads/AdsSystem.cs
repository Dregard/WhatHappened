using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Gameplay;
using Zenject;

namespace Game.Ads
{
    public class AdsSystem
    {
        private readonly AdsProvider _adsProvider;
        private readonly GameplaySystem _gameplaySystem;
        private readonly GameplayProgressController _progressController;


        public AdsSystem(AdsProvider adsProvider, GameplaySystem gameplaySystem,
            GameplayProgressController progressController)
        {
            _adsProvider = adsProvider;
            _gameplaySystem = gameplaySystem;
            _progressController = progressController;
        }

        public void TryShowInterstitial(string placement, Action onComplete = null)
        {
            _gameplaySystem.Pause(this);
            _adsProvider.TryShowInterstitial(placement, () =>
            {
                _gameplaySystem.Unpause(this);
                onComplete?.Invoke();
            });
        }

        public async Task<bool> TryShowRewarded(string placement)
        {
            var levelNumber = _progressController.CurrentLevel;
            var levelAttempt = _progressController.CurrentAttempt;
            
            var missionName = $"main_{levelNumber}";
            var rewardAdditionalData = new Dictionary<string, object>
            {
                {
                    "mission_data", new Dictionary<string, object>
                    {
                        { "mission_type", "main" },
                        { "mission_name", missionName },
                        { "mission_id", levelNumber },
                        { "mission_attempt", levelAttempt },
                    }
                },
            };
            
            _gameplaySystem.Pause(this);
            try
            {
                var success = await _adsProvider.TryShowRewarded(placement, rewardAdditionalData);
                return success;
            }
            finally
            {
                _gameplaySystem.Unpause(this);
            }
        }

        public void ShowBanner()
        {
            _adsProvider.ShowBanner();
        }

        public void HideBanner()
        {
            _adsProvider.HideBanner();
        }
    }
}