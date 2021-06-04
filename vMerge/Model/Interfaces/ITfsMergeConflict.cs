using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ITfsMergeConflict
    {
        string ServerPath
        {
            get;
        }

        string Message
        {
            get;
        }

        Conflict Conflict
        {
            get;
        }
    }
}
