using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ITfsChange
    {
        ITfsBranch RootBranch
        {
            get;
        }

        Change Change
        {
            get;
        }

        string ServerItem
        {
            get;
        }
    }
}
