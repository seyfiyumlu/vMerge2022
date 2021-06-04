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
    public class vMergeProfilesPage : DialogPage
    {
        private WinFormProfilesPage Page;

        protected override IWin32Window Window
        {
            get
            {
                if (Page == null)
                    Page = new WinFormProfilesPage();

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
