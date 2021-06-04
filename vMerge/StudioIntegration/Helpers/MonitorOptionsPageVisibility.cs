using alexbegh.Utility.Helpers.WeakReference;
using alexbegh.vMerge.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace alexbegh.vMerge.StudioIntegration.Helpers
{
    public interface IMonitorPageVisibility
    {
        void VisibilityChanged(bool visible);
    }

    public static class MonitorOptionsPageVisibility
    {
        private class BoolWrapper { public bool Value { get; set; } }

        private static WeakReferenceWithMetaDataList<System.Windows.Forms.UserControl, BoolWrapper> _controlsToMonitor = new WeakReferenceWithMetaDataList<System.Windows.Forms.UserControl, BoolWrapper>();
        private static Timer _timer;
        private static object _lock = new object();

        public static void MonitorVisibility<T>(this T control)
            where T : System.Windows.Forms.UserControl, IMonitorPageVisibility
        {
            lock (_lock)
            {
                if (_timer == null)
                {
                    _timer = new Timer(25);
                    _timer.Elapsed += PerformVisibilityCheck;
                    _timer.Start();
                }
                _controlsToMonitor.Add(control, new BoolWrapper() { Value = control.Created });
            }
        }

        static void PerformVisibilityCheck(object sender, ElapsedEventArgs e)
        {
            List<Tuple<System.Windows.Forms.UserControl, BoolWrapper>> controlsToCheck;
            List<Action> triggers = null;
            lock (_lock)
            {
                controlsToCheck = _controlsToMonitor.CompactAndReturn();
                foreach (var control in controlsToCheck)
                {
                    if (control.Item1.Created != control.Item2.Value)
                    {
                        if (triggers == null)
                            triggers = new List<Action>();

                        BoolWrapper wrap = control.Item2;
                        triggers.Add(() => ((IMonitorPageVisibility)control.Item1).VisibilityChanged(wrap.Value = control.Item1.Created));
                    }
                }
            }
            if( triggers!=null)
            {
                Repository.Instance.BackgroundTaskManager.Post(
                    () =>
                    {
                        foreach (var item in triggers)
                            item();
                        return true;
                    });
            }
        }
    }
}
