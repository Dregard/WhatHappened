using DG.Tweening;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Game.Projectiles
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private ProjectileView _view;
        [SerializeField] private float _duration = 1f;
        [SerializeField] private float _maxScale = 1.5f;

        private Action<Projectile> _onComplete;

        public class Pool : MonoMemoryPool<Projectile>
        {
            protected override void OnDespawned(Projectile item)
            {
                item.gameObject.SetActive(false);
                base.OnDespawned(item);
            }
        }

        public UniTask Launch(Texture texture, Sprite sprite, Vector2 startPos, Vector2 endPos, Vector2 size, Action<Projectile> onComplete)
        {
            gameObject.SetActive(true);
            _onComplete = onComplete;
            
            if (sprite != null)
                _view.SetSprite(sprite);
            else
                _view.SetTexture(texture);
            
            var rectTransform = _view.RectTransform;
            rectTransform.anchoredPosition = startPos;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;

            var sequence = DOTween.Sequence();
            
            sequence.Append(rectTransform.DOAnchorPos(endPos, _duration).SetEase(Ease.Linear));
            
            var scaleSequence = DOTween.Sequence();
            scaleSequence.Append(rectTransform.DOScale(_maxScale, _duration / 2f).SetEase(Ease.OutQuad));
            scaleSequence.Append(rectTransform.DOScale(1f, _duration / 2f).SetEase(Ease.InQuad));

            sequence.Join(scaleSequence);
            sequence.OnComplete(() => _onComplete?.Invoke(this));
            
            return sequence.AsyncWaitForCompletion().AsUniTask();
        }
    }
}
