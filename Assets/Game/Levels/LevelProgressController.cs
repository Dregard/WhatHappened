using System;

namespace Game.Levels
{
    public class LevelProgressController
    {
        public event Action TargetReached;

        private int _totalCount;
        private int _remainingItems;
        private bool _isCompletionBlocked;
        private bool _hasCompleted;

        public int RemainingItems => _remainingItems;

        /// <summary>Resets level completion and the presentation gate for a new attempt.</summary>
        public void Start(int totalItems)
        {
            _totalCount = totalItems;
            _remainingItems = totalItems;
            _isCompletionBlocked = false;
            _hasCompleted = false;
        }

        /// <summary>Counts a completed search item without publishing victory while its presentation is pending.</summary>
        public void ItemPlaced()
        {
            _remainingItems--;
            TryComplete();
        }

        /// <summary>Marks gameplay requirements fulfilled; presentation must still finish.</summary>
        public void ForceWin()
        {
            _remainingItems = 0;
            TryComplete();
        }

        /// <summary>Owned by the current search attempt; releasing the gate publishes victory once all items are complete.</summary>
        public void SetCompletionBlocked(bool blocked)
        {
            _isCompletionBlocked = blocked;
            TryComplete();
        }

        /// <summary>Publishes victory once both gameplay and presentation requirements have finished.</summary>
        private void TryComplete()
        {
            if (_remainingItems > 0 || _isCompletionBlocked || _hasCompleted)
                return;
            _hasCompleted = true;
            TargetReached?.Invoke();
        }

        /// <summary>Returns the unfinished shared progress count.</summary>
        public int GetRemainingCount()
        {
            return _remainingItems;
        }

        /// <summary>Returns completed progress in the range zero to one; an uninitialized attempt reports zero.</summary>
        public float GetLevelProgress()
        {
            return _totalCount > 0 ? 1f - _remainingItems / (float)_totalCount : 0f;
        }
    }
}
