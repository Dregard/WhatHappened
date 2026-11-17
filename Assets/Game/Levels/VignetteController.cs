using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Levels
{
    public class VignetteController : MonoBehaviour
    {
        [SerializeField] private Image _vignetteImage;
        [SerializeField] private float _pulseDuration = 1f;
        [SerializeField] private float _minAlpha = 0f;
        [SerializeField] private float _maxAlpha = 0.5f;
        [SerializeField] private Color _timeOutColor = Color.red;
        [SerializeField] private Color _timefreezeColor = Color.cornflowerBlue;
        [SerializeField] private Ease _ease = Ease.InOutQuad;

        private Sequence _sequence;


        private void Awake()
        {
            Stop();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }
        
        public void Play(VignetteKind kind)
        {
            Stop();

            _vignetteImage.color = kind switch
            {
                VignetteKind.TimeOut => _timeOutColor,
                VignetteKind.TimeFreeze => _timefreezeColor,
            };
            _vignetteImage.color = new Color(_vignetteImage.color.r, _vignetteImage.color.g, _vignetteImage.color.b,
                _minAlpha);
            
            _sequence = DOTween.Sequence();
            _sequence.Append(_vignetteImage.DOFade(_maxAlpha, _pulseDuration / 2f).SetEase(_ease));
            _sequence.Append(_vignetteImage.DOFade(_minAlpha, _pulseDuration / 2f).SetEase(_ease));
        }

        public void ShowStatic(VignetteKind kind)
        {
            Stop();

            _vignetteImage.color = kind switch
            {
                VignetteKind.TimeOut => _timeOutColor,
                VignetteKind.TimeFreeze => _timefreezeColor,
            };
            _vignetteImage.color = new Color(_vignetteImage.color.r, _vignetteImage.color.g, _vignetteImage.color.b,
                _minAlpha);
            
            _sequence = DOTween.Sequence();
            _sequence.Append(_vignetteImage.DOFade(_maxAlpha, _pulseDuration / 2f).SetEase(_ease));
        }

        public void Stop()
        {
            if (_sequence != null)
            {
                _sequence.Kill();
                _sequence = null;
            }

            var color = _vignetteImage.color;
            color.a = _minAlpha;
            _vignetteImage.color = color;
        }
    }
}