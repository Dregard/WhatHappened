using Game.UI.Windows.General;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.PreloadWindow
{
    public class PreloadWindow : Window<PreloadWindowModel>
    {
        [SerializeField] private Image _image;
        [SerializeField] private Image _progressFill;

        private PreloadWindowModel _model;
        private RectTransform _imageRectTransform;
        private RectTransform _parentRectTransform;
        private RectTransform _progressRectTransform;
        private float _progressFullAnchorMaxX;
        private float _progressFullSizeDeltaX;

        /// <summary>Caches the authored full-width layout before hiding the initial progress.</summary>
        private void Awake()
        {
            _imageRectTransform = _image.rectTransform;
            _parentRectTransform = transform.parent as RectTransform;
            _progressRectTransform = _progressFill.rectTransform;
            _progressFullAnchorMaxX = _progressRectTransform.anchorMax.x;
            _progressFullSizeDeltaX = _progressRectTransform.sizeDelta.x;
            DisplayProgress(0f);
        }

        /// <summary>Binds this preparation's progress without retaining the previous window state.</summary>
        protected override void Setup(PreloadWindowModel model)
        {
            Unsubscribe();
            _model = model;
            _model.ProgressChanged += DisplayProgress;
            DisplayProgress(_model.Progress);
            AdjustImageSize();
        }

        /// <summary>Releases the model subscription with the window lifetime.</summary>
        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>Disconnects the previous preparation before destruction or rebinding.</summary>
        private void Unsubscribe()
        {
            if (_model != null)
                _model.ProgressChanged -= DisplayProgress;
            _model = null;
        }

        /// <summary>Scales the authored horizontal span and insets with progress; anchors keep it proportional during resizing.</summary>
        private void DisplayProgress(float progress)
        {
            var amount = Mathf.Clamp01(progress);
            var anchorMax = _progressRectTransform.anchorMax;
            anchorMax.x = Mathf.Lerp(_progressRectTransform.anchorMin.x, _progressFullAnchorMaxX, amount);
            _progressRectTransform.anchorMax = anchorMax;
            var sizeDelta = _progressRectTransform.sizeDelta;
            sizeDelta.x = _progressFullSizeDeltaX * amount;
            _progressRectTransform.sizeDelta = sizeDelta;
            _progressFill.enabled = amount > 0f;
        }

        /// <summary>Keeps the splash covering the parent when the screen size changes.</summary>
        private void OnRectTransformDimensionsChange()
        {
            AdjustImageSize();
        }

        /// <summary>Preserves the existing full-screen image aspect and cropping.</summary>
        private void AdjustImageSize()
        {
            if (_image == null || _image.sprite == null || _parentRectTransform == null)
                return;

            var screenWidth = _parentRectTransform.rect.width;
            var screenHeight = _parentRectTransform.rect.height;

            var imageWidth = _image.sprite.rect.width;
            var imageHeight = _image.sprite.rect.height;

            var screenRatio = screenWidth / screenHeight;
            var imageRatio = imageWidth / imageHeight;

            if (screenRatio > imageRatio)
            {
                var scale = screenWidth / imageWidth;
                _imageRectTransform.sizeDelta = new Vector2(screenWidth, imageHeight * scale);
            }
            else
            {
                var scale = screenHeight / imageHeight;
                _imageRectTransform.sizeDelta = new Vector2(imageWidth * scale, screenHeight);
            }

            _imageRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _imageRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _imageRectTransform.pivot = new Vector2(0.5f, 0.5f);
            _imageRectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
