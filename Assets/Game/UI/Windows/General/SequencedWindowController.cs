using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.UI.Windows.Controllers;
using UnityEngine;

namespace Game.UI.Windows.General
{
    public class SequencedWindowController : WindowController
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform[] _transformsQueue;
        [SerializeField] private float _scaleDuration;
        [SerializeField] private AnimationCurve _scaleUpCurve;
        [SerializeField] private AnimationCurve _scaleDownCurve;
        [SerializeField] private float _closeDuration;
        [SerializeField] private AnimationCurve _closeCurve;

        private Sequence _animationSequence;


        protected override void Awake()
        {
            base.Awake();

            foreach (var rectTransform in _transformsQueue)
            {
                rectTransform.localScale = Vector3.zero;
            }
        }

        protected override UniTask AnimateOpen()
        {
            SetInteractable(false);

            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();

            foreach (var rectTransform in _transformsQueue)
            {
                _animationSequence.Append(rectTransform.DOScale(Vector3.one, _scaleDuration).SetEase(_scaleUpCurve));
            }

            _animationSequence.OnComplete(() =>
            {
                SetInteractable(true);
            });

            return _animationSequence.AsyncWaitForCompletion().AsUniTask();
        }

        protected override UniTask AnimateClose()
        {
            SetInteractable(false);
            
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();

            for (var i = _transformsQueue.Length - 1; i >= 0 ; i--)
            {
                var rectTransform = _transformsQueue[i];
                _animationSequence.Append(rectTransform.DOScale(Vector3.zero, _scaleDuration).SetEase(_scaleDownCurve));
            }

            _animationSequence.OnComplete(() =>
            {
                SetInteractable(true);
            });

            return _animationSequence.AsyncWaitForCompletion().AsUniTask();
        }

        private void SetInteractable(bool isInteractable)
        {
            _canvasGroup.interactable = isInteractable;
        }
    }
}