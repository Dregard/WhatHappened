using System;
using Game.SFX;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.UI.Buttons
{
    [RequireComponent(typeof(Button))]
    public class GameButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        
        [Inject] private SignalBus _signalBus;
        [Inject] private AudioSystem _audioSystem;
        
        public bool IsInteractable { get; protected set; }

        public event Action ClickEvent;
        public event Action<bool> InteractivityChangeEvent;


        protected virtual void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            SetInteractable(button.interactable);

            button.onClick.AddListener(OnClick);
        }

        public void SetInteractable(bool isInteractable)
        {
            IsInteractable = isInteractable;
            
            button.interactable = isInteractable;

            InteractivityChangeEvent?.Invoke(isInteractable);
        }

        protected virtual void OnClick()
        {
            if (UnityEngine.Input.touchCount > 1)
                return;

            _signalBus.Fire<Signals.ButtonClickedSignal>(new Signals.ButtonClickedSignal
            {
                IsInteractable = IsInteractable
            });
            
            if (IsInteractable == false)
                return;

            _audioSystem.PlaySfx("ButtonClick");
            
            ClickEvent?.Invoke();
        }
    }
}