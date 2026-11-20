using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Unscaled alpha transitions shared by the stages of the chapter result window.</summary>
    public static class ChapterFade
    {
        /// <summary>Fades a group's alpha to the target over the given seconds; zero applies it at once.</summary>
        public static async UniTask To(CanvasGroup group, float target, float seconds, CancellationToken token)
        {
            var start = group.alpha;
            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, elapsed / seconds));
                await UniTask.NextFrame(token);
            }
            group.alpha = target;
        }

        /// <summary>Fades an object out through its own group, then deactivates it. The group is created on demand.</summary>
        public static async UniTask Out(GameObject target, float seconds, CancellationToken token)
        {
            if (!target.activeSelf)
                return;
            var group = Group(target);
            group.interactable = false;
            group.blocksRaycasts = false;
            await To(group, 0f, seconds, token);
            target.SetActive(false);
        }

        /// <summary>Returns the object's group, adding one when missing.</summary>
        public static CanvasGroup Group(GameObject target)
        {
            return target.TryGetComponent<CanvasGroup>(out var group) ? group : target.AddComponent<CanvasGroup>();
        }
    }
}
