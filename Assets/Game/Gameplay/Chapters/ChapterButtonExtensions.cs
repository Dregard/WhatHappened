using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.UI.Buttons;

namespace Game.Gameplay.Chapters
{
    public static class ChapterButtonExtensions
    {
        /// <summary>Waits for a single press of a button, or the optional timeout when a positive one is supplied.</summary>
        public static async UniTask WaitPress(this GameButton button, CancellationToken token, float timeout = -1f)
        {
            var source = new UniTaskCompletionSource();
            Action release = () => source.TrySetResult();
            button.ClickEvent += release;
            try
            {
                if (timeout > 0)
                    await UniTask.WhenAny(source.Task, UniTask.Delay(TimeSpan.FromSeconds(timeout), ignoreTimeScale: true, cancellationToken: token));
                else
                    await source.Task.AttachExternalCancellation(token);
            }
            finally
            {
                if (button != null)
                    button.ClickEvent -= release;
            }
        }
    }
}
