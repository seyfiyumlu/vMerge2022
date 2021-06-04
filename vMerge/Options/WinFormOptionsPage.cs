using alexbegh.vMerge.Model;
using alexbegh.vMerge.StudioIntegration.Helpers;
using alexbegh.vMerge.ViewModel.Options;
using System.Windows.Controls;

namespace alexbegh.vMerge.Options
{
    public partial class WinFormOptionsPage : System.Windows.Forms.UserControl, IMonitorPageVisibility
    {
        private GeneralOptionsViewModel _optionsVm;
        private ContentControl _page;

        public WinFormOptionsPage()
        {
            InitializeComponent();

            _optionsVm = new GeneralOptionsViewModel();
            _page = Repository.Instance.ViewManager.CreateViewFor(_optionsVm).View;
            wpfControl.Child = _page;
            this.MonitorVisibility();
        }

        public void Save()
        {
            _optionsVm.RaiseFinished(true);
        }

        public void Reset()
        {
            _optionsVm = new GeneralOptionsViewModel();
            _page.DataContext = _optionsVm;
        }

        public void VisibilityChanged(bool visible)
        {
            if (visible)
                Repository.Instance.ViewManager.AddUnmanagedParent(this);
            else
                Repository.Instance.ViewManager.RemoveUnmanagedParent(this);
        }
    }
}
