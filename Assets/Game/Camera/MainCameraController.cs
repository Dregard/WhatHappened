using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zenject;

namespace Game.Camera
{
    public class MainCameraController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _camera;

        public UnityEngine.Camera Camera => _camera;
        
        private float _targetDistance;
        private float _currentDistance;

        
        [Inject]
        private void Construct(IEnumerable<IAdditionalCameraProvider> providers)
        {
            if (providers == null)
                return;
            
            var cameraData = _camera.GetUniversalAdditionalCameraData();
            cameraData.cameraStack.Clear();

            var additionalCameraProviders = providers as IAdditionalCameraProvider[] ?? providers.ToArray();
            var cameras = additionalCameraProviders.SelectMany(p => p.CameraDatas)
                .OrderBy(p => p.Priority)
                .Select(p => p.Camera);
            foreach (var additionalCamera in cameras)
            {
                cameraData.cameraStack.Add(additionalCamera);
            }
        }
    }
}
