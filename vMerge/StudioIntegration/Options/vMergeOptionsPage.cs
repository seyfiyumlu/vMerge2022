using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using alexbegh.vMerge.Options;

namespace alexbegh.vMerge.StudioIntegration.Options
{
    public class vMergeOptionsPage : DialogPage
    {
        private WinFormOptionsPage Page;

        protected override IWin32Window Window
        {
            get
            {
                if (Page == null)
                    Page = new WinFormOptionsPage();

                return Page as IWin32Window;
            }
        }

        protected override void OnApply(DialogPage.PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                Page.Save();
            }
            else
            {
                Page.Reset();
            }
        }
    }
}
