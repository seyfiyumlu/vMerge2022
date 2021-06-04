using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.StudioIntegration.Framework
{
    class vMergeUIProvider : IVMergeUIProvider
    {
        private vMergePackage vMergePackage
        {
            get;
            set;
        }

        public vMergeUIProvider(vMergePackage package)
        {
            vMergePackage = package;
        }

        public void FocusChangesetWindow()
        {
            vMergePackage.ShowChangesetView();
        }

        public void FocusWorkItemWindow()
        {
            vMergePackage.ShowWorkItemView();
        }

        public void FocusMergeWindow()
        {
            vMergePackage.ShowMergeView();
        }

        public bool IsMergeWindowVisible()
        {
            return vMergePackage.MergeToolWindowIsVisible;
        }

        public event EventHandler MergeWindowVisibilityChanged
        {
            add
            {
                vMergePackage.MergeToolWindowVisibilityChanged += value;
            }
            remove
            {
                vMergePackage.MergeToolWindowVisibilityChanged -= value;
            }
        }
    }
}
