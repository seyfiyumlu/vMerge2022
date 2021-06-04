using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.Model.Implementation
{
    public class TfsPendingChange : ITfsPendingChange
    {
        public PendingChange PendingChange
        {
            get;
            private set;
        }

        public string ServerPath
        {
            get;
            private set;
        }

        internal TfsPendingChange(PendingChange pendingChange)
        {
            PendingChange = pendingChange;
            ServerPath = pendingChange.ServerItem;
        }
    }
}
