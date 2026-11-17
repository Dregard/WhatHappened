using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Game.UI.Notifications
{
    public sealed class TextNotificationSystem : IInitializable, IDisposable
    {
        private readonly TextNotificationView _view;
        private readonly Queue<TextNotificationData> _notificationsQueue = new();
        private TextNotificationData _current;
        private bool _isInitialized;
        private bool _isDisposed;
        private UniTaskCompletionSource _drainedSource;

        /// <summary>Receives the presentation owned by the HUD from the composition root.</summary>
        public TextNotificationSystem(TextNotificationView view)
        {
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
        }

        /// <summary>Enables presentation and starts entries queued before Zenject initialization.</summary>
        public void Initialize()
        {
            _isInitialized = true;
            ShowNext();
        }

        /// <summary>Queues text in arrival order. The action runs after its entrance, never during enqueue.</summary>
        public void Enqueue(string text, Action onAppeared = null)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TextNotificationSystem));
            _notificationsQueue.Enqueue(new TextNotificationData(text, onAppeared));
            ShowNext();
        }

        /// <summary>Queues text with an arrival callback. Cancellation or disposal never invokes the callback.</summary>
        public void EnqueueWithArrival(string text, Action onArrived)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TextNotificationSystem));
            _notificationsQueue.Enqueue(new TextNotificationData(text, null, onArrived));
            ShowNext();
        }

        /// <summary>Starts one entry only when the previous presentation has completely ended.</summary>
        private void ShowNext()
        {
            if (!_isInitialized || _isDisposed || _current != null || _notificationsQueue.Count == 0)
                return;
            _current = _notificationsQueue.Dequeue();
            _view.Show(_current.Text, HandleAppeared, HandleCompleted);
        }

        /// <summary>Consumes the action before invoking it, allowing reentrant enqueue and isolating caller exceptions.</summary>
        private void HandleAppeared()
        {
            var action = _current?.OnAppeared;
            if (_current != null)
                _current = new TextNotificationData(_current.Text, null, _current.OnArrived);
            try
            {
                action?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>Consumes the arrival action once, isolates caller exceptions, and advances the FIFO after arrival.</summary>
        private void HandleCompleted()
        {
            var action = _current?.OnArrived;
            _current = null;
            try
            {
                action?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            ShowNext();
            if (_current == null && _notificationsQueue.Count == 0)
            {
                var source = _drainedSource;
                _drainedSource = null;
                source?.TrySetResult();
            }
        }

        /// <summary>Completes when the current and queued entries finish or are cancelled, including entries added while waiting. Disposal cancels waiters.</summary>
        public UniTask WaitAllFinished()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TextNotificationSystem));
            if (_current == null && _notificationsQueue.Count == 0)
                return UniTask.CompletedTask;
            _drainedSource ??= new UniTaskCompletionSource();
            return _drainedSource.Task;
        }

        /// <summary>Cancels current and queued presentation without invoking their callbacks. Completes drain waiters and permits later entries.</summary>
        public void CancelAll()
        {
            if (_isDisposed)
                return;
            _notificationsQueue.Clear();
            _current = null;
            _view.Stop();
            var source = _drainedSource;
            _drainedSource = null;
            source?.TrySetResult();
        }

        /// <summary>Cancels presentation and releases queued actions when the scene container is disposed.</summary>
        public void Dispose()
        {
            _isDisposed = true;
            _drainedSource?.TrySetCanceled();
            _drainedSource = null;
            _notificationsQueue.Clear();
            _current = null;
            if (_view != null)
                _view.Stop();
        }
    }
}
