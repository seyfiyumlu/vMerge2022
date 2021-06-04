using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace alexbegh.vMerge.View
{
    /// <summary>
    /// Interaction logic for StatusSplashPopup.xaml
    /// </summary>
    public partial class StatusSplashPopup : DialogWindow
    {
        private DispatcherTimer _timer;
        public DateTime _start;
        private double _maxTransparency = 0.80;
        private double oldWidth = 0.0;
        private double oldHeight = 0.0;
        public Action ShowOptionPage;
        public Action ShowDownload;

        public StatusSplashPopup()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(0.1);
            _timer.Tick += TimerTick;
            _start = DateTime.Now;
            _timer.Start();
            MouseDown += Window_MouseDown;
            OkayIcon.Visibility = System.Windows.Visibility.Visible;
            FailureIcon.Visibility = System.Windows.Visibility.Collapsed;
            SizeChanged += Window_SizeChanged;
        }

        private void Window_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            Left = Left + oldWidth - ActualWidth;
            Top = Top + oldHeight - ActualHeight;
            oldWidth = ActualWidth;
            oldHeight = ActualHeight;
        }

        public void SetGracePeriodMode()
        {
            _maxTransparency = 1.0;
            OkayIcon.Visibility = System.Windows.Visibility.Collapsed;
            FailureIcon.Visibility = System.Windows.Visibility.Visible;
            var brushConverter = new BrushConverter();
            BackBorder.Background = (Brush)brushConverter.ConvertFrom("#FFBBA404");
            OptionsLink.Visibility = System.Windows.Visibility.Visible;
        }

        public void SetUpdateAvailableMode()
        {
            _maxTransparency = 1.0;
            OkayIcon.Visibility = System.Windows.Visibility.Collapsed;
            FailureIcon.Visibility = System.Windows.Visibility.Visible;
            var brushConverter = new BrushConverter();
            BackBorder.Background = (Brush)brushConverter.ConvertFrom("#FFBBA404");
            NewVersionLink.Visibility = System.Windows.Visibility.Visible;
        }

        public void SetTrialPeriodExpiredMode()
        {
            OkayIcon.Visibility = System.Windows.Visibility.Collapsed;
            FailureIcon.Visibility = System.Windows.Visibility.Collapsed;
            var brushConverter = new BrushConverter();
            BackBorder.Background = (Brush)brushConverter.ConvertFrom("#FF50634A");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - _start;
            if (elapsed.TotalMilliseconds < 1000)
            {
                Opacity = ((double)elapsed.TotalMilliseconds) / (1000 / _maxTransparency);
            }
            else if (elapsed.TotalMilliseconds > 13000)
            {
                _timer.Stop();
                _timer.Tick -= TimerTick;
                Close();
            }
            else if (elapsed.TotalMilliseconds > 10000)
            {
                Opacity = Math.Max((13000.0 - elapsed.TotalMilliseconds) / (3000 / _maxTransparency), 0);
            }
        }

        private void Options_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowOptionPage();
        }

        private void Download_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowDownload();
        } 
    }
}
