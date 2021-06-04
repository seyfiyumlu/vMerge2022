using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface IProfileSettings
    {
        string Name { get; }
        string TeamProject { get; }
        string TeamProjectFriendlyName { get; }
        string WISourceBranch { get; set; }
        string WITargetBranch { get; set; }
        string WISourcePathFilter { get; set; }
        string WIQueryName { get; set; }
        string CSSourceBranch { get; set; }
        string CSTargetBranch { get; set; }
        string CSSourcePathFilter { get; set; }
        string CSQueryName { get; set; }
        string ChangesetIncludeCommentFilter { get; set; }
        string ChangesetExcludeCommentFilter { get; set; }
        string ChangesetIncludeUserFilter { get; set; }
        DateTime? DateFromFilter { get; set; }
        DateTime? DateToFilter { get; set; }

        void CopyTo(IProfileSettings other);
    }
}
