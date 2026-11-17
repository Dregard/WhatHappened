using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Boosters;
using UnityEngine;
using Zenject;

namespace Game.UI.Hud
{
    public class HudModel
    {
        public DiContainer Container;
        
        public Action PauseAction;
        public Func<float> GetTimeFunc;
        
        public IEnumerable<Booster> Boosters;
        public Func<BoosterKind, int> GetBoosterCountFunc;
        public Func<BoosterKind, bool> CanUseBoosterFunc;
        public Func<bool> IsSearchStartedFunc;
        public Action<BoosterKind> UseBoosterAction;
        public Action<Action<BoosterKind, int>> SubscribeOnBoosterCountChanged;
        
        public event Action SubscribeOnLevelStarted;
        public event Action<string> TutorialButtonRequest;
        public event Action TutorialHideRequest;
        
        public Func<BoosterKind, bool> IsBoosterUnlockedFunc { get; set; }
        
        public int CurrentLevel;
        
        public void InvokeLevelStarted()
        {
            SubscribeOnLevelStarted?.Invoke();
        }
        
        public void InvokeTutorialButtonRequest(string tutorialId)
        {
            TutorialButtonRequest?.Invoke(tutorialId);
        }
        
        public void InvokeTutorialHideRequest()
        {
            TutorialHideRequest?.Invoke();
        }
    }
}
