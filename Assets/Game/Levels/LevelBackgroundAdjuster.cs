using UnityEngine;

namespace Game.Levels
{
    public class LevelBackgroundAdjuster : MonoBehaviour
    {
        public enum AdjustmentMode
        {
            Cover,
            FitWidthTop
        }

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private AdjustmentMode _adjustmentMode = AdjustmentMode.Cover;

        private void Start()
        {
            Adjust();
        }

        public void Adjust()
        {
            if (_spriteRenderer == null || _camera == null)
            {
                Debug.LogWarning("LevelBackgroundAdjuster: SpriteRenderer or Camera is not assigned!");
                return;
            }

            if (_spriteRenderer.sprite == null)
            {
                Debug.LogWarning("LevelBackgroundAdjuster: Sprite is not assigned to SpriteRenderer!");
                return;
            }

            // Get camera dimensions in world units
            float cameraHeight = _camera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * _camera.aspect;

            // Get sprite dimensions in world units
            Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

            if (_adjustmentMode == AdjustmentMode.Cover)
            {
                // Calculate scale factors
                float scaleX = cameraWidth / spriteSize.x;
                float scaleY = cameraHeight / spriteSize.y;

                // Use the larger scale factor to ensure the sprite covers the entire camera area (Cover effect)
                // While maintaining the aspect ratio
                float targetScale = Mathf.Max(scaleX, scaleY);

                _spriteRenderer.transform.localScale = new Vector3(targetScale, targetScale, 1f);
            }
            else if (_adjustmentMode == AdjustmentMode.FitWidthTop)
            {
                // Calculate scale based on width
                float targetScale = cameraWidth / spriteSize.x;
                _spriteRenderer.transform.localScale = new Vector3(targetScale, targetScale, 1f);

                // Align to top
                Vector3 cameraPosition = _camera.transform.position;
                float topEdgeY = cameraPosition.y + _camera.orthographicSize;
                
                // Sprite's pivot is usually at center, so we need half height after scaling
                float spriteHalfHeight = (spriteSize.y * targetScale) / 2f;
                
                Vector3 newPosition = _spriteRenderer.transform.position;
                newPosition.y = topEdgeY - spriteHalfHeight;
                _spriteRenderer.transform.position = newPosition;
            }
        }
    }
}
