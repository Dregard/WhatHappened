using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Rotates a sunburst layer at the configured speed; zero leaves it still.</summary>
    public sealed class ChapterSunburstSpinner : MonoBehaviour
    {
        private float _turnSeconds;

        public void Initialize(float turnSeconds)
        {
            _turnSeconds = turnSeconds;
            transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (_turnSeconds > 0)
                transform.Rotate(0, 0, -360f / _turnSeconds * Time.unscaledDeltaTime);
        }
    }
}
