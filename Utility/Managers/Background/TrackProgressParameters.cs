using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace alexbegh.Utility.Managers.Background
{
    /// <summary>
    /// The kind of desired progress tracking
    /// </summary>
    public enum TrackProgressMode
    {
        /// <summary>
        /// Just measure (sets TrackProgress.MaxProgress)
        /// </summary>
        MeasureOnly,

        /// <summary>
        /// Perform action, but don't increment CurrentProgress (ProgressInfoText may be set)
        /// </summary>
        PerformWithoutIncrements,

        /// <summary>
        /// Perform action and increment
        /// </summary>
        PerformWithIncrements,

        /// <summary>
        /// Perform action, increment and set maximum value
        /// </summary>
        PerformWithIncrementsAndSetMaximum
    }

    /// <summary>
    /// The class bundles relevant information for actions which support reporting
    /// progress information.
    /// </summary>
    public class TrackProgressParameters
    {
        #region Private Classes
        private class TrackProgressWithoutIncrementsWrapper : ITrackProgress
        {
            public TrackProgressWithoutIncrementsWrapper(ITrackProgress wrapped)
            {
                Wrapped = wrapped;
            }

            ITrackProgress Wrapped { get; set; }

            public double MaxProgress
            {
                get
                {
                    return Wrapped.MaxProgress;
                }
                set
                {
                }
            }

            public double CurrentProgress
            {
                get
                {
                    return Wrapped.CurrentProgress;
                }
                set
                {
                }
            }

            public string ProgressInfo
            {
                get
                {
                    return Wrapped.ProgressInfo;
                }
                set
                {
                    Wrapped.ProgressInfo = value;
                }
            }

            public bool HasChangedSinceLastGetForCurrentThread()
            {
                return Wrapped.HasChangedSinceLastGetForCurrentThread();
            }

            public ProgressData GetCurrentProgress()
            {
                return Wrapped.GetCurrentProgress();
            }

            public void Increment()
            {
            }
        }
        #endregion

        /// <summary>
        /// Constructs an instance.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="trackProgress"></param>
        /// <param name="cancellationToken"></param>
        internal TrackProgressParameters(TrackProgressMode mode, ITrackProgress trackProgress, CancellationToken cancellationToken)
        {
            if (trackProgress == null)
                throw new ArgumentNullException("trackProgress");
            TrackProgressMode = mode;
            TrackProgress = trackProgress;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Constructs an instance with a pre-initialized ITrackProgress implementation
        /// </summary>
        /// <param name="mode">The mode</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public TrackProgressParameters(TrackProgressMode mode, CancellationToken cancellationToken)
        {
            TrackProgressMode = mode;
            TrackProgress = new BackgroundTask.TrackProgressImpl();
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Constructs an instance with a pre-initialized ITrackProgress implementation just for measurement
        /// </summary>
        /// <param name="mode">The mode</param>
        public TrackProgressParameters(TrackProgressMode mode)
        {
            if (mode == TrackProgressMode.MeasureOnly)
                throw new ArgumentException("mode");

            TrackProgressMode = mode;
            TrackProgress = new BackgroundTask.TrackProgressImpl();
            CancellationToken = default(CancellationToken);
        }

        /// <summary>
        /// Track progress mode
        /// </summary>
        public TrackProgressMode TrackProgressMode
        { get; private set; }

        /// <summary>
        /// Track progress interface
        /// </summary>
        public ITrackProgress TrackProgress
        { get; private set; }

        /// <summary>
        /// Cancellation token
        /// </summary>
        public CancellationToken CancellationToken
        { get; private set; }

        /// <summary>
        /// Returns a deep copy of this instance with its TrackProgressMode set to "MeasureOnly"
        /// </summary>
        /// <returns></returns>
        public TrackProgressParameters GetMeasureParameters()
        {
            var copy = new alexbegh.Utility.Managers.Background.BackgroundTask.TrackProgressImpl();
            copy.MaxProgress = TrackProgress.MaxProgress;
            copy.CurrentProgress = TrackProgress.CurrentProgress;
            copy.ProgressInfo = TrackProgress.ProgressInfo;

            return new TrackProgressParameters(Background.TrackProgressMode.MeasureOnly, copy, CancellationToken); 
        }

        /// <summary>
        /// Checks if this parameter packs' mode is set so that TrackProgress.MaxProgress is expected to be set
        /// </summary>
        /// <returns>true if MaxProgress should be set</returns>
        public bool ShouldSetMaximumValue()
        {
            return TrackProgressMode == TrackProgressMode.MeasureOnly
                || TrackProgressMode==TrackProgressMode.PerformWithIncrementsAndSetMaximum;
        }
        
        /// <summary>
        /// Checks if this parameter packs' mode is set to execute the action
        /// </summary>
        /// <returns>true if action should be executed</returns>
        public bool ShouldExecute()
        {
            return TrackProgressMode != TrackProgressMode.MeasureOnly;
        }

        /// <summary>
        /// Checks if this parameter packs' mode is set to increment CurrentProgress
        /// </summary>
        /// <returns>true if CurrentProgress should be incremented</returns>
        public bool ShouldIncrement()
        {
            return TrackProgressMode == TrackProgressMode.PerformWithIncrements
                || TrackProgressMode == TrackProgressMode.PerformWithIncrementsAndSetMaximum;
        }

        /// <summary>
        /// Clone this instance and return one which only allows to set "ProgressInfo"
        /// </summary>
        /// <returns>A cloned instance</returns>
        public TrackProgressParameters CloneWithoutIncrements()
        {
            TrackProgressParameters copy
                = new TrackProgressParameters(
                            Background.TrackProgressMode.PerformWithoutIncrements,
                            new TrackProgressWithoutIncrementsWrapper(this.TrackProgress),
                            this.CancellationToken);
            return copy;
        }
    }
}
