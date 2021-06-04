using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ITfsBranch
    {
        BranchObject BranchObject
        {
            get;
        }

        string Name
        {
            get;
        }

        List<string> ChildBranchNames
        {
            get;
        }

        List<ITfsBranch> ChildBranches
        {
            get;
        }

        string ServerPath
        {
            get;
        }

        bool IsSubBranch
        {
            get;
        }
    }
}
