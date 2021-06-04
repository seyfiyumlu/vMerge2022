using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public struct MergedChangeset
    {
        public ITfsChangeset Source { get; set; }
        public ITfsChangeset Target { get; set; }
    }

    public interface ITfsChangeset
    {
        #region Properties
        /// <summary>
        /// The underlying tfs changeset
        /// </summary>
        Changeset Changeset 
        { 
            get; 
        }

        /// <summary>
        /// The title of the changeset
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        /// Is the property "Changes" already loaded?
        /// </summary>
        bool HasChangesLoaded
        {
            get;
        }

        /// <summary>
        /// The list of related work items
        /// </summary>
        IReadOnlyList<ITfsWorkItem> RelatedWorkItems
        {
            get;
        }

        /// <summary>
        /// The list of merge source changesets
        /// </summary>
        IReadOnlyList<ITfsChangeset> SourceChangesets
        {
            get;
        }

        /// <summary>
        /// The list of changed items
        /// </summary>
        IReadOnlyList<ITfsChange> Changes
        {
            get;
        }
        #endregion

        #region Operations
        /// <summary>
        /// Checks if this changeset has been merged into <paramref name="target"/> already
        /// </summary>
        /// <param name="target">The target branch</param>
        /// <returns>The merge changeset if any, otherwise null</returns>
        ITfsChangeset HasBeenMergedInto(ITfsBranch target);

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

        /// <summary>
        /// Returns all merged changesets for this changeset in the active project
        /// </summary>
        /// <param name="source">The source branch (from within the changeset) to search merges for</param>
        /// <param name="potentialMergeSourceBranches">If null, search in all branches of the active project</param>
        /// <returns></returns>
        IEnumerable<MergedChangeset> FindMergesForActiveProject(ITfsBranch source, IEnumerable<ITfsBranch> potentialMergeSourceBranches = null);
        #endregion
    }
}
