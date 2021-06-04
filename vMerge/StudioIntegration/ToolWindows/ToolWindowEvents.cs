using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using alexbegh.vMerge.StudioIntegration.Framework;
using alexbegh.vMerge.Model;

namespace alexbegh.vMerge.StudioIntegration.ToolWindows
{
    public sealed class ToolWindowEvents : IVsWindowFrameNotify3, IDisposable
    {
        public Func<uint, int> OnClose { get; set; }
        public Func<bool, int, int, int, int, int> OnDockableChange { get; set; }
        public Func<int, int, int, int, int> OnMove { get; set; }
        public Func<__FRAMESHOW, int> OnShow { get; set; }
        public Func<int, int, int, int, int> OnSize { get; set; }
        private IVsWindowFrame _frame;
        private bool _disposed;

        public ToolWindowEvents(IVsWindowFrame frame)
        {
            _frame = frame;
            Repository.Instance.BackgroundTaskManager.DelayedPost(DetectClosedWindow);
        }

        bool DetectClosedWindow()
        {
            if (_disposed)
                return true;

            if (VSConstants.S_FALSE == _frame.IsVisible())
            {
                _frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                Dispose();
            }

            return false;
        }

        int IVsWindowFrameNotify3.OnClose(ref uint pgrfSaveOptions)
        {
            if (OnClose != null)
                return OnClose(pgrfSaveOptions);
            return VSConstants.S_OK;
        }

        int IVsWindowFrameNotify3.OnDockableChange(int fDockable, int x, int y, int w, int h)
        {
            if (OnDockableChange != null)
                return OnDockableChange(fDockable != 0, x, y, w, h);
            return VSConstants.S_OK;
        }

        int IVsWindowFrameNotify3.OnMove(int x, int y, int w, int h)
        {
            if (OnMove != null)
                return OnMove(x, y, w, h);
            return VSConstants.S_OK;
        }

        [PrincipalPermission(SecurityAction.Demand)]
        int IVsWindowFrameNotify3.OnShow(int fShow)
        {
            if (OnShow != null)
                return OnShow((__FRAMESHOW)fShow);
            //_setWindowVisibility(
            //    ((__FRAMESHOW)fShow
            //    != __FRAMESHOW.FRAMESHOW_WinHidden));
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsWindowFrameNotify3.OnSize(int x, int y, int w, int h)
        {
            if (OnSize != null)
                return OnSize(x, y, w, h);
            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
