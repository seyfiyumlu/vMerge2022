namespace alexbegh.Utility.Managers.Background
{
    /// <summary>
    /// Public interface to get/set progress information from.
    /// <see cref="BackgroundTaskManager"/>, <see cref="alexbegh.Utility.UserControls.LoadingProgress.LoadingProgressViewModel"/>
    /// </summary>
    public interface ITrackProgress
    {
        /// <summary>
        /// The maximum progress
        /// </summary>
        double MaxProgress { get; set; }

        /// <summary>
        /// The current progress
        /// </summary>
        double CurrentProgress { get; set; }

        /// <summary>
        /// Progress info string
        /// </summary>
        string ProgressInfo { get; set; }

        /// <summary>
        /// Checks if the data has changed since the last call from this thread to <see cref="GetCurrentProgress"/>
        /// </summary>
        /// <returns>true if changed</returns>
        bool HasChangedSinceLastGetForCurrentThread();

        /// <summary>
        /// Returns a (atomically taken) snapshot from the current progress
        /// </summary>
        /// <returns>The current progress</returns>
        ProgressData GetCurrentProgress();

        /// <summary>
        /// Increments "CurrentProgress"
        /// </summary>
        void Increment();
    }
}
