using System.Collections.Generic;
using System.Linq;
using Game.Tutorial;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.UI.Hud
{
    public class HudTutorialHandler : MonoBehaviour, IInitializable
    {
        [SerializeField] private Image _backgroundImage;
        
        private SignalBus _signalBus;
        
        private Dictionary<string, TutorialButton> _buttonsById;

        private TutorialButton _currentButton;
        private Canvas _currentCanvas;


        private void Awake()
        {
            _buttonsById = GetComponentsInChildren<TutorialButton>().ToDictionary(k => k.ButtonId, v => v);
        }
        
        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
        
        public void Initialize()
        {
            _signalBus.Subscribe<Signals.ShowButtonRequested>(HandleButtonRequested);
            _signalBus.Subscribe<Signals.ButtonClicked>(HandleButtonClicked);
        }

        private void ShowButton(string id)
        {
            if (_buttonsById.TryGetValue(id, out var button) == false)
            {
                Debug.LogError($"Button with id {id} not found");
                return;
            }
            
            _backgroundImage.gameObject.SetActive(true);
            _currentButton = button;
            _currentCanvas = _currentButton.gameObject.AddComponent<Canvas>();
        }

        private void HandleButtonRequested(Signals.ShowButtonRequested signal)
        {
            
        }
        
        private void HandleButtonClicked(Signals.ButtonClicked signal)
        {
            
        }
    }
}