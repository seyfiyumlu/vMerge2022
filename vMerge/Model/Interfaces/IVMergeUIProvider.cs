using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    interface IVMergeUIProvider
    {
        void FocusChangesetWindow();
        void FocusWorkItemWindow();
        void FocusMergeWindow();

        bool IsMergeWindowVisible();

        event EventHandler MergeWindowVisibilityChanged;
    }
}
