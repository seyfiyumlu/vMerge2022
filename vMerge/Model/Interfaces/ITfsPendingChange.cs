using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ITfsPendingChange
    {
        string ServerPath
        {
            get;
        }

        PendingChange PendingChange
        {
            get;
        }
    }
}
