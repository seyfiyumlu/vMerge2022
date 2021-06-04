using alexbegh.Utility.Helpers.Atomic;
using alexbegh.Utility.UserControls.LoadingProgress;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace alexbegh.Utility.Managers.Background
{
    /// <summary>
    /// This class encapsulates data for the BackgroundTaskManager class.
    /// </summary>
    public class BackgroundTask
    {
        #region Internal classes
        /// <summary>
        /// This internal class implements ITrackProgress using AtomicMutable.
        /// It may be accessed by any thread.
        /// </summary>
        internal class TrackProgressImpl : AtomicMutable<ProgressData>, ITrackProgress
        {
            #region Public Operations
            /// <summary>
            /// Returns the current progress info (as an atomic operation)
            /// </summary>
            /// <returns></returns>
            public ProgressData GetCurrentProgress()
            {
                ProgressData res = new ProgressData();
                GetAndReset(ref res);
                return res;
            }

            /// <summary>
            /// Increments the CurrentProgress property
            /// </summary>
            public void Increment()
            {
                AcquireWrite();
                ++Data.CurrentProgress;
                ReleaseWrite();
            }
            #endregion

            #region Public Properties
            public double MaxProgress
            {
                get
                {
                    AcquireRead();
                    var res = Data.MaxProgress;
                    ReleaseRead();
                    return res;
                }
                set
                {
                    AcquireWrite();
                    Data.MaxProgress = value;
                    ReleaseWrite();
                }
            }

            public double CurrentProgress
            {
                get
                {
                    AcquireRead();
                    var res = Data.CurrentProgress;
                    ReleaseRead();
                    return res;
                }
                set
                {
                    AcquireWrite();
                    Data.CurrentProgress = value;
                    ReleaseWrite();
                }
            }

            public string ProgressInfo
            {
                get
                {
                    AcquireRead();
                    var res = Data.ProgressInfo;
                    ReleaseRead();
                    return res;
                }
                set
                {
                    AcquireWrite();
                    Data.ProgressInfo = value;
                    ReleaseWrite();
                }
            }
            #endregion
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Cancel support
        /// </summary>
        public CancellationTokenSource Cancelled { get; set; }

        /// <summary>
        /// The .NET task
        /// </summary>
        public Task Task { get; internal set; }

        /// <summary>
        /// The synchronization context of the caller
        /// </summary>
        public SynchronizationContext Ctx { get; private set; }

        /// <summary>
        /// The progress Tracker (to be updated by the contained task)
        /// </summary>
        private TrackProgressImpl _trackProgress;

        /// <summary>
        /// The progress tracker property
        /// </summary>
        public ITrackProgress TrackProgress { get { return _trackProgress; } }
        #endregion

        #region Internal Properties
        /// <summary>
        /// The progress indicator view model (may be null)
        /// </summary>
        internal List<LoadingProgressViewModel> LoadingProgressViewModels { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates an BackgroundTask
        /// </summary>
        /// <param name="source">The manager</param>
        /// <param name="loadingProgressViewModel">The progress indicator view model</param>
        /// <param name="key">The key for the task (only one parallel task per key may run simultaneously)</param>
        /// <param name="ctx">The callers' synchronization context</param>
        internal BackgroundTask(BackgroundTaskManager source, LoadingProgressViewModel loadingProgressViewModel, string key, SynchronizationContext ctx)
        {
            Ctx = ctx;
            LoadingProgressViewModels = new List<LoadingProgressViewModel>();
            LoadingProgressViewModels.Add(loadingProgressViewModel);
            Cancelled = new CancellationTokenSource();
            _trackProgress = new TrackProgressImpl();
        }
        #endregion

        #region Public Operations
        /// <summary>
        /// Sends an action back to the callers' synchronization context
        /// </summary>
        /// <param name="x"></param>
        public void SendBack(Action x)
        {
            Ctx.Send((o) => x(), null);
        }

        /// <summary>
        /// Posts an action back to the callers' synchronization context
        /// </summary>
        /// <param name="x"></param>
        public void PostBack(Action x)
        {
            Ctx.Post((o) => x(), null);
        }
        #endregion
    }
}
