using System;

namespace Game.UI.Notifications
{
    public sealed class TextNotificationData
    {
        public string Text { get; }
        public Action OnAppeared { get; }
        public Action OnArrived { get; }

        /// <summary>Creates a queue entry; the optional action runs once when its entrance animation completes.</summary>
        public TextNotificationData(string text, Action onAppeared = null)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            OnAppeared = onAppeared;
        }

        /// <summary>Creates an entry with an optional arrival action, consumed only after the text finishes its flight.</summary>
        public TextNotificationData(string text, Action onAppeared, Action onArrived) : this(text, onAppeared)
        {
            OnArrived = onArrived;
        }
    }
}
