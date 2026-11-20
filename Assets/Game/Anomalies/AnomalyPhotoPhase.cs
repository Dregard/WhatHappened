namespace Game.Anomalies
{
    /// <summary>Stage of one photo-booster view; the board stays blocked and the level clock suspended in every stage except None.</summary>
    public enum AnomalyPhotoPhase
    {
        None,
        /// <summary>The photo flies from the thumbnail to the center; no countdown, no early close.</summary>
        Opening,
        /// <summary>The photo is fully open: the five-second countdown runs and a tap may close it early.</summary>
        Showing,
        /// <summary>The photo flies back to the thumbnail; taps are ignored until it arrives.</summary>
        Closing,
    }
}
