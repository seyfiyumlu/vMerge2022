using alexbegh.Utility.Managers.Background;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace alexbegh.Utility.Windows
{
    /// <summary>
    /// Interaction logic for WaitForBackgroundActionDialog.xaml
    /// </summary>
    public partial class WaitForBackgroundActionDialog : Microsoft.VisualStudio.PlatformUI.DialogWindow
    {
        private ITrackProgress ProgressTracker { get; set; }
        private DispatcherTimer Timer { get; set; }
        private bool Cancelled { get; set; }
        private CancellationTokenSource CancelToken { get; set; }
        private Task Task { get; set; }

        internal WaitForBackgroundActionDialog(ITrackProgress progressTracker,CancellationTokenSource cancelToken, Task task)
        {
            InitializeComponent();
            ProgressTracker = progressTracker;
            CancelToken = cancelToken;
            Task = task;
            Timer = new DispatcherTimer(DispatcherPriority.Render);
            Timer.Interval = TimeSpan.FromMilliseconds(50);
            Timer.Tick += TimerTick;
            Timer.Start();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        /// <summary>
        /// OnClosed event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Timer.Stop();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (Task.IsCompleted || Task.IsCanceled || Task.IsFaulted)
                Close();
            if (Cancelled)
            {
                ((this.FindName("Text")) as TextBlock).Text = "Cancelling ...";
            }
            else
            {
                var data = ProgressTracker.GetCurrentProgress();
                if (data.MaxProgress != 0)
                {
                    Percent.Visibility = Visibility.Visible;
                    Percent.Text = Math.Max(Math.Min(1, (data.CurrentProgress / data.MaxProgress)), 0).ToString("P");
                }
                if (!String.IsNullOrWhiteSpace(data.ProgressInfo))
                    ((this.FindName("Text")) as TextBlock).Text = data.ProgressInfo;
            }
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            CancelToken.Cancel();
            Cancelled = true;
            (this.FindName("CancelButton") as Button).IsEnabled = false;
        }


    }
}
