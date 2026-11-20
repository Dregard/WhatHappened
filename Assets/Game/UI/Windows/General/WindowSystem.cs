using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Camera;
using Game.UI.Windows.Controllers;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.UI.Windows.General
{
    [RequireComponent(typeof(Canvas))]
    public class WindowSystem : MonoBehaviour, IAdditionalCameraProvider
    {
        public event Action WindowOpenedEvent;
        public event Action WindowClosedEvent;
        
        [SerializeField] private List<WindowBase> _windowPrefabs;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private float _showDuration = 0.5f;
        [SerializeField] private float _openAlpha = 0.8f;
        [SerializeField] private AnimationCurve _openCurve;
        [SerializeField] private AnimationCurve _closeCurve;
        
        private readonly Dictionary<Type, WindowBase> _windowPrefabsByType = new();
        private readonly Dictionary<Type, WindowController> _currentControllersByType = new();
        private readonly List<WindowController> _currentControllers = new();
        
        private readonly Queue<WindowQueueElementData> _windowsQueue = new();
        
        private Dictionary<Type, IWindowModelProcessor> _modelProcessorsByType = new();
        
        private int _nextWindowSortingOrder = 1;
        private DiContainer _diContainer;
        private Tween _tween;


        

        public AdditionalCameraData[] CameraDatas => new AdditionalCameraData[]
        {
            new AdditionalCameraData
            {
                Camera = _canvas.worldCamera,
                Priority = 10,
            },
        };

        public int OpenWindowsCount => _currentControllers.Count;

        private void Awake()
        {
            foreach (var windowPrefab in _windowPrefabs)
            {
                _windowPrefabsByType.Add(windowPrefab.GetType(), windowPrefab);
            }
            
            _backgroundImage.color = new Color(.0f, .0f, .0f, .0f);
        }

        [Inject]
        public void Construct(IEnumerable<IWindowModelProcessor> modelProcessors, DiContainer diContainer)
        {
            _modelProcessorsByType = modelProcessors
                .ToDictionary(p => p.GetModelType(), p => p);
            _diContainer = diContainer;
        }
        
        /// <summary>Opens the window; optionally rebinds supplied state when an earlier operation already opened it.</summary>
        public async UniTask Open<TWindow>(ModelParams modelParams = null, bool refreshModel = false)
            where TWindow : WindowBase
        {
            var windowType = typeof(TWindow);
            await Open(windowType, modelParams, refreshModel);
        }

        /// <summary>Creates and presents a window, or replaces an existing window's model only when explicitly requested.</summary>
        private async UniTask Open(Type windowType, ModelParams modelParams = null, bool refreshModel = false)
        {
            if (_windowPrefabsByType.ContainsKey(windowType) == false)
            {
                Debug.LogError($"Window {windowType.Name} not found in prefabs");
                return;
            }

            if (_currentControllersByType.Count == 0)
            {
                _tween?.Kill();
                _tween = _backgroundImage.DOFade(_openAlpha, _showDuration)
                    .SetEase(_openCurve);
            }

            if (_currentControllersByType.ContainsKey(windowType) == false)
            {
                var tWindow = CreateWindow(windowType).GetComponent<WindowBase>();
                var modelType = tWindow.GetModelType();
                if (_modelProcessorsByType.TryGetValue(modelType, out var processor) == false)
                {
                    Debug.LogError($"Model processor for {windowType.Name} not found");
                    return;
                }
                
                processor.SetParams(modelParams);
                var model = processor.GetModel();
                tWindow.Setup(model);
                var controller = _currentControllersByType[windowType];

                SetWindowSorting(controller);

                await controller.Open();
                
                WindowOpenedEvent?.Invoke();
            }
            else if (refreshModel && modelParams != null)
            {
                var window = _currentControllersByType[windowType].GetComponent<WindowBase>();
                var processor = _modelProcessorsByType[window.GetModelType()];
                processor.SetParams(modelParams);
                window.Setup(processor.GetModel());
            }
        }
        
        public void AddToQueue<TWindow>(ModelParams modelParams = null)
        {
            if (_currentControllersByType.Count > 0)
            {
                _windowsQueue.Enqueue(new WindowQueueElementData
                {
                    WindowType = typeof(TWindow),
                    ModelParams = modelParams,
                });
            }
            else
            {
                Open(typeof(TWindow), modelParams).Forget();
            }
        }

        public async UniTask Close<TWindow>()
        {
            var windowType = typeof(TWindow);
            
            await Close(windowType);
        }

        private async UniTask Close(Type windowType)
        {
            if (_currentControllersByType.TryGetValue(windowType, out var windowController) == false)
            {
                Debug.LogWarning($"Window {windowType.Name} not opened");
                return;
            }

            await windowController.Close();
                    
            Destroy(windowController.gameObject);
                    
            _currentControllersByType.Remove(windowType);
            _currentControllers.Remove(windowController);
                    
            CheckForAllWindowsClosed();
            
            WindowClosedEvent?.Invoke();
        }

        public async UniTask CloseAll()
        {
            foreach (var windowController in _currentControllers.ToArray())
            {
                await Close(windowController.WindowType);
            }
        }

        private GameObject CreateWindow(Type windowType)
        {
            var original = _windowPrefabsByType[windowType];
            var window = _diContainer.InstantiatePrefab(original, transform);

            var controller = window.GetComponent<WindowController>();
            controller.Adjust();

            _currentControllersByType.Add(windowType, controller);
            _currentControllers.Add(controller);

            return window;
        }

        private void SetWindowSorting(WindowController windowController)
        {
            windowController.gameObject.layer = gameObject.layer;

            var canvas = windowController.Canvas;
            
            canvas.overrideSorting = true;
            canvas.sortingLayerID = _canvas.sortingLayerID;
            canvas.sortingOrder = _nextWindowSortingOrder;
            
            _nextWindowSortingOrder++;
        }

        private void CheckForAllWindowsClosed()
        {
            if (_currentControllersByType.Count > 0) 
                return;
            
            _nextWindowSortingOrder = 1;
            if (_windowsQueue.Count == 0)
            {
                _tween?.Kill();
                _tween = _backgroundImage.DOFade(.0f, _showDuration)
                    .SetEase(_closeCurve);
                return;
            }
                
            var data = _windowsQueue.Dequeue();
            Open(data.WindowType, data.ModelParams).Forget();
        }

        public async UniTask WaitAllClosed()
        {
            await UniTask.WaitUntil(() => _currentControllersByType.Count == 0 && _windowsQueue.Count == 0);
        }

        public void CloseTopWindow()
        {
            if (_currentControllers.Count == 0)
                return;
            
            var controller = _currentControllers.Last();
            if (controller.CanCloseByReturn)
                Close(controller.WindowType).Forget();
        }
    }
}
