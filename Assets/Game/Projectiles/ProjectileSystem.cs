using Cysharp.Threading.Tasks;
using Game.Camera;
using UnityEngine;

namespace Game.Projectiles
{
    public class ProjectileSystem
    {
        private readonly Projectile.Pool _pool;
        private readonly UnityEngine.Camera _mainCamera;

        
        public ProjectileSystem(
            Projectile.Pool pool,
            MainCameraController cameraController)
        {
            _pool = pool;
            _mainCamera = cameraController.Camera;
        }

        public UniTask LaunchWorldToCanvas(Texture texture, Sprite sprite, Vector3 startPosition, Vector3 endPosition, Canvas canvas, RectTransform canvasRect, Vector2 size)
        {
            Vector2 screenPoint = _mainCamera.WorldToScreenPoint(startPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvas.worldCamera, out var localStartPos);
            
            Vector2 localEndPoint = canvasRect.InverseTransformPoint(endPosition);

            var projectile = _pool.Spawn();
            projectile.transform.SetParent(canvas.transform, false);
            
            return projectile.Launch(texture, sprite, localStartPos, localEndPoint, size, p => _pool.Despawn(p));
        }

        public UniTask LaunchCanvasToCanvas(Texture texture, Sprite sprite, Vector3 startPosition, Vector3 endPosition, RectTransform canvasRect, Vector2 size)
        {
            Vector2 localStartPos = canvasRect.InverseTransformPoint(startPosition);
            
            Vector2 localEndPoint = canvasRect.InverseTransformPoint(endPosition);

            var projectile = _pool.Spawn();
            projectile.transform.SetParent(canvasRect, false);
            projectile.gameObject.layer = canvasRect.gameObject.layer;
            
            return projectile.Launch(texture, sprite, localStartPos, localEndPoint, size, p => _pool.Despawn(p));
        }
    }
}
