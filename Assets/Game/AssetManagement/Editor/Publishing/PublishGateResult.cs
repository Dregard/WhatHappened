namespace Game.AssetManagement.Publishing
{
    /// <summary>Decision of the publication gate. IsWarning marks skips that the user probably wants to notice.</summary>
    public sealed class PublishGateResult
    {
        public bool ShouldPublish { get; private set; }
        public bool IsWarning { get; private set; }
        public string Reason { get; private set; }

        private PublishGateResult(bool shouldPublish, bool isWarning, string reason)
        {
            ShouldPublish = shouldPublish;
            IsWarning = isWarning;
            Reason = reason;
        }

        /// <summary>All conditions hold; publication may start.</summary>
        public static PublishGateResult Allow()
        {
            return new PublishGateResult(true, false, string.Empty);
        }

        /// <summary>Publication must not start; the reason is logged (as a warning when the skip is unexpected).</summary>
        public static PublishGateResult Skip(string reason, bool isWarning)
        {
            return new PublishGateResult(false, isWarning, reason);
        }
    }
}
