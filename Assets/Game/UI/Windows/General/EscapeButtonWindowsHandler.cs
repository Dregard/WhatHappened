using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Game.UI.Windows.General
{
    public class EscapeButtonWindowsHandler : MonoBehaviour
    {
        [SerializeField] private InputAction _escapeAction;
        
        [Inject] private WindowSystem _windowSystem;


        private void OnEnable()
        {
            _escapeAction.Enable();
            _escapeAction.performed += PerformAction;
        }

        private void OnDisable()
        {
            _escapeAction.Disable();
            _escapeAction.performed -= PerformAction;
        }

        private void PerformAction(InputAction.CallbackContext callbackContext)
        {
            _windowSystem.CloseTopWindow();
        }
    }
}