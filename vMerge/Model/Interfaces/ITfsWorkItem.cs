using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ITfsWorkItem
    {
        WorkItem WorkItem
        {
            get;
        }

        int Id
        {
            get;
        }

        DateTime ChangedDate
        {
            get;
        }

        string Title
        {
            get;
        }

        int RelatedChangesetCount
        {
            get;
        }

        List<ITfsChangeset> RelatedChangesets
        {
            get;
        }


        /// <summary>
        /// Returns the branches affected by this changeset (of the active project)
        /// </summary>
        /// <returns>Affected branches</returns>
        IEnumerable<ITfsBranch> GetAffectedBranchesForActiveProject();

        /// <summary>
        /// Returns the changes for the given branch
        /// </summary>
        /// <returns>List of changes</returns>
        IReadOnlyList<ITfsChange> GetChangesForBranch(ITfsBranch branch);

        /// <summary>
        /// Returns all changes
        /// </summary>
        /// <returns>Related changes</returns>
        IEnumerable<ITfsChange> GetAllChanges();
    }
}
