using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>One step of the chapter result window. It owns only its own elements; things that persist across steps live in ChapterResultShared and are placed through Layout. The window prepares the stage while it is transparent, fades it in (while Intro may animate), awaits Play (which ends on the player's button press and its exit animation) and later fades it out.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ChapterResultStage : MonoBehaviour
    {
        private CanvasGroup _group;

        protected ChapterResultShared Shared { get; private set; }

        private CanvasGroup Group
        {
            get { return _group != null ? _group : _group = GetComponent<CanvasGroup>(); }
        }

        /// <summary>Where the shared elements belong while this stage is shown.</summary>
        public abstract ChapterSharedLayout Layout { get; }

        /// <summary>Fills the stage from the current model while it is fully transparent and not interactive.</summary>
        public void Begin(ChapterResultWindowModel model, ChapterResultShared shared)
        {
            Shared = shared;
            Group.alpha = 0f;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            gameObject.SetActive(true);
            Prepare(model);
        }

        /// <summary>Reveals the prepared stage; input is accepted only after it is fully visible.</summary>
        public async UniTask FadeIn(float seconds, CancellationToken token)
        {
            Group.blocksRaycasts = true;
            await ChapterFade.To(Group, 1f, seconds, token);
            Group.interactable = true;
        }

        /// <summary>Blocks input, fades the whole stage out and deactivates it.</summary>
        public async UniTask FadeOut(float seconds, CancellationToken token)
        {
            Group.interactable = false;
            Group.blocksRaycasts = false;
            await ChapterFade.To(Group, 0f, seconds, token);
            gameObject.SetActive(false);
        }

        /// <summary>Builds this stage's content from the model.</summary>
        protected abstract void Prepare(ChapterResultWindowModel model);

        /// <summary>Optional motion that runs while the stage fades in.</summary>
        public virtual UniTask Intro(ChapterResultWindowModel model, CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>Runs the stage until its continuation is requested and its exit animation is done.</summary>
        public abstract UniTask Play(ChapterResultWindowModel model, CancellationToken token);
    }
}
