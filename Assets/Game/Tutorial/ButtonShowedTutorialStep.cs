using UnityEngine;

namespace Game.Tutorial
{
    public class ButtonShowedTutorialStep : TutorialStep
    {
        [SerializeField] private GameObject _tutorialObject;
        
        
        protected override void OnStepStarted()
        {
        }

        protected override void ExecuteInternal()
        {
            if (_tutorialObject != null)
                _tutorialObject.SetActive(true);
        }

        protected override void CompleteInternal()
        {
            if (_tutorialObject != null)
                _tutorialObject.SetActive(false);
        }
    }
}