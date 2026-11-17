using UnityEngine;
using UnityEngine.UI;

namespace Game.Projectiles
{
    public class ProjectileView : MonoBehaviour
    {
        [SerializeField] private RawImage _image;
        [SerializeField] private Image _spriteImage;
        [SerializeField] private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform;

        public void SetTexture(Texture texture)
        {
            _image.texture = texture;
            _image.gameObject.SetActive(true);
            
            if (_spriteImage != null)
                _spriteImage.gameObject.SetActive(false);
        }

        public void SetSprite(Sprite sprite)
        {
            if (_spriteImage != null)
            {
                _spriteImage.sprite = sprite;
                _spriteImage.gameObject.SetActive(true);
                _image.gameObject.SetActive(false);
            }
            else
            {
                SetTexture(sprite.texture);
            }
        }
    }
}
