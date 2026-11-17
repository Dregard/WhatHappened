using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Game.Effects
{
    public class TextEffect : Effect
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _duration = 1.5f;
        [SerializeField] private float _moveDistance = 3.0f;
        [SerializeField] private AnimationCurve _curve;

        private Sequence _sequence;

        public override float GetDuration() => _duration;

        public void SetText(string text)
        {
            _text.text = text;
        }

        public void SetScore(int score)
        {
            _text.text = $"+{score}";
        }

        public override void Play()
        {
            _sequence?.Kill();

            var color = _text.color;
            color.a = 1;
            _text.color = color;

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOMoveY(transform.position.y + _moveDistance, _duration));
            _sequence.Join(_text.DOFade(0, _duration));
            _sequence.SetEase(_curve);
            _sequence.OnComplete(Finish);
        }

        public override void ResetEffect()
        {
            base.ResetEffect();
            _sequence?.Kill();
        }
    }
}