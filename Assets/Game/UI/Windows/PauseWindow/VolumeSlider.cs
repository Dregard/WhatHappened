using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.PauseWindow
{
    public class VolumeSlider : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private GameObject _sliderSegment;
        [SerializeField] private RectTransform _segmentsContainer;

        public event Action<float> OnVolumeChanged;

        private void Awake()
        {
            _slider.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnDestroy()
        {
            _slider.onValueChanged.RemoveListener(HandleValueChanged);
        }
        
        public void SetVolume(float modelCurrentSoundVolume)
        {
            _slider.value = modelCurrentSoundVolume * _slider.maxValue;
        }

        private void HandleValueChanged(float value)
        {
            var normalizedVolume = value / _slider.maxValue;
            OnVolumeChanged?.Invoke(normalizedVolume);
        }

        [ContextMenu("Rebuild Segments")]
        private void RebuildSegments()
        {
            if (this == null || _slider == null || _sliderSegment == null || _segmentsContainer == null) return;

            for (var i = _segmentsContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(_segmentsContainer.GetChild(i).gameObject);
            }

            var count = (int)(_slider.maxValue - _slider.minValue);
            if (count <= 0) return;

            for (var i = 0; i <= count; i++)
            {
                var segment = Instantiate(_sliderSegment, _segmentsContainer);
                var segmentRect = segment.GetComponent<RectTransform>();
                
                var t = i / (float)count;
                segmentRect.anchorMin = new Vector2(t, 0.5f);
                segmentRect.anchorMax = new Vector2(t, 0.5f);
                segmentRect.anchoredPosition = Vector2.zero;
            }
        }
    }
}
