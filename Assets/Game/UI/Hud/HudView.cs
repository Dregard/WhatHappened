using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Camera;
using Game.Gameplay.Chapters;
using Game.UI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Hud
{
    public class HudView : MonoBehaviour, IAdditionalCameraProvider
    {
        public Canvas HudCanvas { get { return _canvas; } }
        public ChapterHudView ChapterHud { get { return _chapterHud; } }
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private GameButton _pauseButton;
        [SerializeField] private GameObject _timerContainer;
        [SerializeField] private HudTimer _timer;
        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private RectTransform _boostersContainer;
        [SerializeField] private List<BoosterButtonView> _boosterButtons;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _blockerImage;
        [SerializeField] private float _boostersOffset;
        [SerializeField] private ChapterHudView _chapterHud;
        [SerializeField] private GameObject _searchGatedBoosterWidget;

        private Dictionary<string, TutorialButton> _tutorialButtonsById;
        private TutorialButton _currentTutorialButton;
        
        private Action _pauseAction;
        private Func<float> _getTimerValue;
        private BoosterButtonsController _boosterButtonsController;
        private Func<bool> _isSearchStartedFunc;
        private bool _isGatedWidgetAvailable;


        public AdditionalCameraData[] CameraDatas => new AdditionalCameraData[]
        {
            new AdditionalCameraData
            {
                Camera = _camera,
                Priority = 5,
            },
        };

        private void Awake()
        {
            _blockerImage.color = new Color(.0f, .0f, .0f, 0);
            
            var tutorialButtons = GetComponentsInChildren<TutorialButton>(true);
            _tutorialButtonsById = tutorialButtons.ToDictionary(button => button.ButtonId);
        }

        private void Update()
        {
            var time = _getTimerValue();
            _timer.SetTime(time);
            var isTimed = time > 0;
            if (_timerContainer.activeSelf != isTimed)
                _timerContainer.SetActive(isTimed);
            if (_pauseButton.gameObject.activeSelf != isTimed)
                _pauseButton.gameObject.SetActive(isTimed);

            _boosterButtonsController?.RefreshButtonsInteractable();
            if (_isGatedWidgetAvailable)
            {
                var isSearchStarted = _isSearchStartedFunc == null || _isSearchStartedFunc();
                if (_searchGatedBoosterWidget.activeSelf != isSearchStarted)
                    _searchGatedBoosterWidget.SetActive(isSearchStarted);
            }
        }
        
        public void Setup(HudModel model)
        {
            _pauseAction = model.PauseAction;
            _pauseButton.ClickEvent += Pause;
            _getTimerValue = model.GetTimeFunc;

            _boosterButtonsController = new BoosterButtonsController(_boosterButtons, model, model.IsBoosterUnlockedFunc);
            _boosterButtonsController.Initialize();
            _isSearchStartedFunc = model.IsSearchStartedFunc;
            _isGatedWidgetAvailable = _searchGatedBoosterWidget != null && _searchGatedBoosterWidget.activeSelf;

            model.SubscribeOnLevelStarted += () =>
            {
                UpdateLevelText(model.CurrentLevel);
            };
            
            model.TutorialButtonRequest += HandleTutorialButtonRequest;
            model.TutorialHideRequest += HandleTutorialHideRequest;

            UpdateLevelText(model.CurrentLevel);
        }

        private void UpdateLevelText(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Level {level}";
            }
        }

        private void HandleTutorialButtonRequest(string id)
        {
            if (_tutorialButtonsById.TryGetValue(id, out var button))
            {
                _currentTutorialButton = button;
                var canvas = _currentTutorialButton.gameObject.AddComponent<Canvas>();
                _currentTutorialButton.gameObject.AddComponent<GraphicRaycaster>();
                canvas.overrideSorting = true;
                canvas.sortingLayerID = _canvas.sortingLayerID;
                canvas.sortingOrder = _canvas.sortingOrder + 100;
                _blockerImage.DOFade(.7f, .2f);
                _blockerImage.raycastTarget = true;
            }
        }

        private void HandleTutorialHideRequest()
        {
            if (_currentTutorialButton != null)
            {
                Destroy(_currentTutorialButton.gameObject.GetComponent<GraphicRaycaster>());
                Destroy(_currentTutorialButton.gameObject.GetComponent<Canvas>());
                
                _currentTutorialButton = null;
                _blockerImage.DOFade(0, .2f);
                _blockerImage.raycastTarget = false;
            }
        }

        private void Pause()
        {
            _pauseAction?.Invoke();
        }

        public void ResetHud()
        {
            _timer.ResetTimer();
        }
    }
}
