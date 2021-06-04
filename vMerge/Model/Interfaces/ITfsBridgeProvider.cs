using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using alexbegh.Utility.Managers.Background;

namespace alexbegh.vMerge.Model.Interfaces
{
    enum BranchSearchOptions
    {
        Upwards,
        Downwards,
        Both
    }

    interface ITfsBridgeProvider : INotifyPropertyChanged
    {
        #region Internal and Private Properties
        TfsTeamProjectCollection TfsTeamProjectCollection 
        { 
            get; 
        }

        VersionControlServer VersionControlServer
        {
            get;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The currently active team project
        /// </summary>
        TeamProject ActiveTeamProject
        {
            get;
            set;
        }

        /// <summary>
        /// The full list of available branches for the current project
        /// </summary>
        ObservableCollection<ITfsBranch> CompleteBranchList
        {
            get;
        }

        /// <summary>
        /// The root query item
        /// </summary>
        ITfsQueryFolder RootQuery
        {
            get;
        }

        /// <summary>
        /// The team project has been selected
        /// </summary>
        event EventHandler ActiveProjectSelected;

        /// <summary>
        /// The property CompleteBranchList is up to date
        /// </summary>
        event EventHandler CompleteBranchListLoaded;

        /// <summary>
        /// The property CompleteBranchList is up to date and CompleteBranchList events have been processed
        /// </summary>
        event EventHandler AfterCompleteBranchListLoaded;

        /// <summary>
        /// The property RootQueryLoaded is up to date
        /// </summary>
        event EventHandler RootQueryLoaded;

        /// <summary>
        /// The branch list is loading
        /// </summary>
        event EventHandler CompleteBranchListLoading;

        /// <summary>
        /// The root query is loading
        /// </summary>
        event EventHandler RootQueryLoading;
        #endregion

        #region Operations
        /// <summary>
        /// Returns the list of possible (direct) merge target branches
        /// </summary>
        /// <param name="sourceBranch">The source branch</param>
        /// <returns>List of target branches</returns>
        IEnumerable<ITfsBranch> GetPossibleMergeTargetBranches(ITfsBranch sourceBranch, BranchSearchOptions searchOptions = BranchSearchOptions.Both);

        /// <summary>
        /// Returns a branch by specifying the fully qualified TFS source control path
        /// </summary>
        /// <param name="sourceBranch">The source branches path</param>
        /// <returns>The TFS branch, null if no branch exists</returns>
        ITfsBranch GetBranchByNameOrNull(string sourceBranch);

        /// <summary>
        /// Tries to find the matching item in the target branch.
        /// </summary>
        /// <param name="targetBranch">The target branch</param>
        /// <param name="serverItem">The item</param>
        /// <returns>The serverItem path in the target branch if found, null otherwise</returns>
        string GetPathInTargetBranch(ITfsBranch targetBranch, string serverItem);

        /// <summary>
        /// Acquires the list of possible merge candidates from the given source to destination branch
        /// </summary>
        /// <param name="source">The source branch</param>
        /// <param name="destination">The target branch</param>
        /// <returns>List of changesets</returns>
        IEnumerable<ITfsChangeset> GetMergeCandidatesForBranchToBranch(ITfsBranch source, ITfsBranch destination, string sourcePathFilter = null);

        /// <summary>
        /// Acquires the full list of changesets from the given branch
        /// </summary>
        /// <param name="branch">The branch</param>
        /// <returns>List of changesets</returns>
        IEnumerable<ITfsChangeset> GetAllChangesetsForBranch(ITfsBranch branch);

        /// <summary>
        /// Retrieves the list of all (direct) parent changesets which resulted in the given changeset
        /// </summary>
        /// <param name="changeset">Changeset to query</param>
        /// <returns>List of parent changesets</returns>
        IEnumerable<ITfsChangeset> GetMergeSourceChangesets(ITfsChangeset changeset);

        /// <summary>
        /// Retrieves a specific work item by its id
        /// </summary>
        /// <param name="id">The id to query</param>
        /// <returns>The work item, null otherwise</returns>
        ITfsWorkItem GetWorkItemById(int id);

        /// <summary>
        /// Retrieves a specific changeset by its id
        /// </summary>
        /// <param name="id">The id to query</param>
        /// <returns>The changeset, null otherwise</returns>
        ITfsChangeset GetChangesetById(int id);

        /// <summary>
        /// Retrieves the list of associated changesets for a given work item
        /// </summary>
        /// <param name="workItem">The work item</param>
        /// <returns>List of changesets</returns>
        IEnumerable<ITfsChangeset> GetRelatedChangesetsForWorkItem(ITfsWorkItem workItem);

        /// <summary>
        /// Returns the no. of associated changesets for a given work item
        /// </summary>
        /// <param name="workItem">The work item</param>
        /// <returns>No. of changesets</returns>
        int GetRelatedChangesetsForWorkItemCount(ITfsWorkItem workItem);

        /// <summary>
        /// Checks if property CompleteBranchList can be accessed without delay
        /// </summary>
        /// <returns>true/false</returns>
        bool IsCompleteBranchListLoaded();

        /// <summary>
        /// Checks if the property RootQuery can be accessed without delay
        /// </summary>
        /// <returns></returns>
        bool IsRootQueryLoaded();

        /// <summary>
        /// Reinitializes the state
        /// </summary>
        void Clear();

        /// <summary>
        /// Stops all pending actions, refreshes everything
        /// </summary>
        void Refresh();

        /// <summary>
        /// Reloads the branch list
        /// </summary>
        void LoadCompleteBranchList();

        /// <summary>
        /// Returns the list of tfs users for the current project collection
        /// </summary>
        /// <returns>List </returns>
        Microsoft.TeamFoundation.Framework.Client.TeamFoundationIdentity[] GetUsers();

        /// <summary>
        /// Creates a temporary workspace for merging
        /// </summary>
        /// <param name="source">Source branch</param>
        /// <param name="target">Target branch</param>
        /// <returns>Temporary workspace (which must NOT be disposed)</returns>
        ITfsTemporaryWorkspace GetTemporaryWorkspace(ITfsBranch source, ITfsBranch target);

        /// <summary>
        /// Disposes all temporary workspaces
        /// </summary>
        void DisposeTemporaryWorkspaces();
        #endregion

    }
}
