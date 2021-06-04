using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace alexbegh.Utility.UserControls.LoadingProgress.Control
{
    /// <summary>
    /// Interaction logic for LoadingProgressControl.xaml
    /// </summary>
    public partial class LoadingProgressControl : UserControl, INotifyPropertyChanged
    {
        #region Public Enum Types
        /// <summary>
        /// The indicator style (big or small)
        /// </summary>
        public enum ProgressStyle
        {
            /// <summary>
            /// Small indicator
            /// </summary>
            SmallIndicator,

            /// <summary>
            /// Big indicator
            /// </summary>
            BigIndicator
        }
        #endregion

        #region Dependency Properties
        /// <summary>
        /// Dependency property: Indicator style
        /// </summary>
        public static readonly DependencyProperty IndicatorStyleProperty =
            DependencyProperty.Register("IndicatorStyle",
                typeof(ProgressStyle),
                typeof(LoadingProgressControl),
                new PropertyMetadata(
                    ProgressStyle.BigIndicator));

        /// <summary>
        /// Dependency property: Is Loading?
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading",
                typeof(bool),
                typeof(LoadingProgressControl),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    new PropertyChangedCallback(OnIsLoadingChanged)));

        /// <summary>
        /// Dependency property: Max Progress value
        /// </summary>
        public static readonly DependencyProperty MaxProgressProperty =
            DependencyProperty.Register("MaxProgress",
                typeof(double),
                typeof(LoadingProgressControl),
                new PropertyMetadata(double.NaN));

        /// <summary>
        /// Dependency property: Current Progress value
        /// </summary>
        public static readonly DependencyProperty CurrentProgressProperty =
            DependencyProperty.Register("CurrentProgress",
                typeof(double),
                typeof(LoadingProgressControl),
                new PropertyMetadata(double.NaN));

        /// <summary>
        /// Dependency Property: Progress (<see cref="LoadingProgressViewModel"/>
        /// </summary>
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress",
                typeof(LoadingProgressViewModel),
                typeof(LoadingProgressControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    new PropertyChangedCallback(OnProgressPropertyChanged)));
        #endregion

        #region Public Properties
        /// <summary>
        /// The backing property for IndicatorStyle
        /// </summary>
        [Bindable(true)]
        public ProgressStyle IndicatorStyle
        {
            get { return (ProgressStyle)this.GetValue(IndicatorStyleProperty); }
            set { this.SetValue(IndicatorStyleProperty, value); }
        }

        /// <summary>
        /// The backing property for IsLoading
        /// </summary>
        [Bindable(true)]
        public bool IsLoading
        {
            get { return (bool)this.GetValue(IsLoadingProperty); }
            set { this.SetValue(IsLoadingProperty, value); }
        }

        /// <summary>
        /// The backing property for BigIndicatorVisibility
        /// </summary>
        [Bindable(true)]
        public Visibility BigIndicatorVisibility
        {
            get { return (IndicatorStyle == ProgressStyle.BigIndicator) ? (IsLoading ? Visibility.Visible : Visibility.Hidden) : Visibility.Collapsed; }
        }

        /// <summary>
        /// The backing property for BigIndicatorActive
        /// </summary>
        [Bindable(true)]
        public bool BigIndicatorActive
        {
            get { return (IndicatorStyle == ProgressStyle.BigIndicator); }
        }

        /// <summary>
        /// The backing property for SmallIndicatorVisibility
        /// </summary>
        [Bindable(true)]
        public Visibility SmallIndicatorVisibility
        {
            get { return (IndicatorStyle == ProgressStyle.SmallIndicator) ? (IsLoading ? Visibility.Visible : Visibility.Hidden) : Visibility.Collapsed; }
        }

        /// <summary>
        /// The backing property for IndicatorVisibility
        /// </summary>
        [Bindable(true)]
        public Visibility IndicatorVisibility
        {
            get { return IsLoading ? Visibility.Visible : Visibility.Hidden; }
        }

        /// <summary>
        /// The backing property for MaxProgress
        /// </summary>
        [Bindable(true)]
        public double MaxProgress
        {
            get { return (double)this.GetValue(MaxProgressProperty); }
            set { this.SetValue(MaxProgressProperty, value); }
        }

        /// <summary>
        /// The backing property for CurrentProgress
        /// </summary>
        [Bindable(true)]
        public double CurrentProgress
        {
            get { return (double)this.GetValue(CurrentProgressProperty); }
            set { this.SetValue(CurrentProgressProperty, value); }
        }

        /// <summary>
        /// The backing property for Progress (<see cref="LoadingProgressViewModel"/>
        /// </summary>
        [Bindable(true)]
        public LoadingProgressViewModel Progress
        {
            get { return (LoadingProgressViewModel)this.GetValue(ProgressProperty); }
            set { this.SetValue(ProgressProperty, value); }
        }
        #endregion

        #region Private methods
        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LoadingProgressControl).RaisePropertyChanged("IndicatorVisibility");
            (d as LoadingProgressControl).RaisePropertyChanged("BigIndicatorVisibility");
            (d as LoadingProgressControl).RaisePropertyChanged("SmallIndicatorVisibility");
            (d as LoadingProgressControl).RaisePropertyChanged("BigIndicatorActive");
        }

        private static void OnProgressPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var lpc = (d as LoadingProgressControl);
            var vm = (lpc.Progress);

            vm.PropertyChanged += lpc.ProgressViewModelPropertyChanged;
            lpc.IsLoading = vm.IsLoading;
        }

        private void ProgressViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = (sender as LoadingProgressViewModel);
            switch (e.PropertyName)
            {
                case "IsLoading":
                    IsLoading = vm.IsLoading;
                    break;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates the control
        /// </summary>
        public LoadingProgressControl()
        {
            InitializeComponent();
        }
        #endregion

        #region INotifyPropertyChanged
        /// <summary>
        /// The PropertyChanged event
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
