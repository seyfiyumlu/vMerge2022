using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using System;
using System.ComponentModel;

namespace alexbegh.Utility.UserControls.LoadingProgress
{
    /// <summary>
    /// The ViewModel to be used with <see cref="alexbegh.Utility.UserControls.LoadingProgress.Control.LoadingProgressControl"/>
    /// </summary>
    public class LoadingProgressViewModel : NotifyPropertyChangedImpl
    {
        #region Static Constructor
        static LoadingProgressViewModel()
        {
            AddDependency<LoadingProgressViewModel>("MaxProgress", "ProgressPercent", "ProgressText");
            AddDependency<LoadingProgressViewModel>("CurrentProgress", "ProgressPercent", "ProgressText");
            AddDependency<LoadingProgressViewModel>("ProgressInfo", "ProgressText");
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates an instance
        /// </summary>
        public LoadingProgressViewModel()
            : base(typeof(LoadingProgressViewModel))
        {
        }
        #endregion

        #region Properties and Fields
        /// <summary>
        /// The backing field for IsLoading
        /// </summary>
        private bool _isLoading;

        /// <summary>
        /// The IsLoading property
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set { Set(ref _isLoading, value); }
        }

        /// <summary>
        /// The backing field for maximum progress
        /// </summary>
        private double _maxProgress;

        /// <summary>
        /// The maximum progress property
        /// </summary>
        public double MaxProgress
        {
            get { return _maxProgress; }
            set { Set(ref _maxProgress, value); }
        }

        /// <summary>
        /// The backing field for the current progress
        /// </summary>
        private double _currentProgress;

        /// <summary>
        /// The current progress property
        /// </summary>
        public double CurrentProgress
        {
            get { return _currentProgress; }
            set { Set(ref _currentProgress, value); }
        }

        /// <summary>
        /// The backing field for the current progress info text
        /// </summary>
        private string _progressInfo;

        /// <summary>
        /// The current progress info text property
        /// </summary>
        public string ProgressInfo
        {
            get { return _progressInfo; }
            set { Set(ref _progressInfo, value); }
        }

        /// <summary>
        /// The current progress percentage as string
        /// </summary>
        public string ProgressPercent
        {
            get
            {
                if (!double.IsNaN(MaxProgress)
                    && !double.IsNaN(CurrentProgress)
                    && MaxProgress>0)
                {
                    return Math.Max(Math.Min(CurrentProgress / MaxProgress, 1), 0).ToString("P");
                }
                else
                    return String.Empty;
            }
        }

        /// <summary>
        /// Property for returning the current progress as a formatted string
        /// </summary>
        public string ProgressText
        {
            get
            {
                if (!String.IsNullOrEmpty(ProgressPercent)
                    && !String.IsNullOrEmpty(ProgressInfo))
                {
                    return ProgressInfo + "\n" + ProgressPercent;
                }
                else if (!String.IsNullOrEmpty(ProgressPercent))
                    return ProgressPercent;
                else if (!String.IsNullOrEmpty(ProgressInfo))
                    return ProgressInfo;
                else
                    return String.Empty;
            }
        }
        #endregion
    }
}
