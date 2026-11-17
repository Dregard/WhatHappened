using Game.UI.Buttons;
using UnityEngine;
using Zenject;
using Signals = Game.Tutorial.Signals;

namespace Game.UI.Hud
{
    public class TutorialButton : MonoBehaviour
    {
        [SerializeField] private string _buttonId;
        [SerializeField] private GameButton _button;
        [SerializeField] private bool _isAutoActivated;
        
        private SignalBus _signalBus;
        
        
        public string ButtonId => _buttonId;

        private void Start()
        {
            if (_isAutoActivated)
            {
                Activate();
            }
        }
        
        public void Activate()
        {
            _button.ClickEvent += HandleClick;
        }
        
        public void Deactivate()
        {
            _button.ClickEvent -= HandleClick;
        }

        private void HandleClick()
        {
            _signalBus.Fire(new Signals.ButtonClicked
            {
                Id = ButtonId,
            });
        }

        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
    }
}