using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.ViewModel.Options;
using System;
using System.Windows.Controls;
using System.Windows.Interop;

namespace alexbegh.vMerge.View.Options
{
    /// <summary>
    /// Interaction logic for GeneralOptionsView.xaml
    /// </summary>
    [AssociatedViewModel(typeof(GeneralOptionsViewModel))]
    [DoNotStyle()]
    public partial class GeneralOptionsView : UserControl
    {
        private const UInt32 DLGC_WANTARROWS = 0x0001;
        private const UInt32 DLGC_WANTTAB = 0x0002;
        private const UInt32 DLGC_WANTALLKEYS = 0x0004;
        private const UInt32 DLGC_HASSETSEL = 0x0008;
        private const UInt32 DLGC_WANTCHARS = 0x0080;
        private const UInt32 WM_GETDLGCODE = 0x0087;

        public GeneralOptionsView()
        {
            InitializeComponent();

            qbusLink.RequestNavigate += (sender, e) =>
            {
                vMerge.StudioIntegration.Framework.vMergePackage.NavigateToUri(e.Uri);
            };

            Loaded += (o,a) =>
            {
                HwndSource s = HwndSource.FromVisual(this) as HwndSource;
                if (s != null)
                    s.AddHook(new HwndSourceHook(ChildHwndSourceHook));
            };
        }

        private System.IntPtr ChildHwndSourceHook(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETDLGCODE)
            {
                handled = true;
                return new IntPtr(DLGC_WANTCHARS | DLGC_WANTALLKEYS | DLGC_WANTARROWS | DLGC_HASSETSEL);
            }
            return IntPtr.Zero;
        }
    }
}
