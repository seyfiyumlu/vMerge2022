using alexbegh.vMerge.Model;
using alexbegh.vMerge.StudioIntegration.Helpers;
using alexbegh.vMerge.ViewModel.Options;
using System.Windows.Controls;

namespace alexbegh.vMerge.Options
{
    public partial class WinFormProfilesPage : System.Windows.Forms.UserControl, IMonitorPageVisibility
    {
        private ProfilesViewModel _profilesVm;
        private ContentControl _page;

        public WinFormProfilesPage()
        {
            InitializeComponent();

            _profilesVm = new ProfilesViewModel();
            _page = Repository.Instance.ViewManager.CreateViewFor(_profilesVm).View;
            wpfControl.Child = _page;
            this.MonitorVisibility();
        }

        public void Save()
        {
            _profilesVm.RaiseFinished(true);
        }

        public void Reset()
        {
            _profilesVm = new ProfilesViewModel();
            _page.DataContext = _profilesVm;
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
