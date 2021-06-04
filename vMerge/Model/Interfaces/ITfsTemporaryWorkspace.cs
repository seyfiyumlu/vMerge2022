using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using alexbegh.Utility.Managers.Background;
using alexbegh.Utility.UserControls.LoadingProgress;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ITfsTemporaryWorkspace : IDisposable
    {
        Workspace TfsWorkspace
        {
            get;
        }

        IReadOnlyList<ITfsMergeConflict> Conflicts
        {
            get;
        }

        IReadOnlyList<ITfsPendingChange> PendingChanges
        {
            get;
        }

        LoadingProgressViewModel PendingChangesLoadingProgress
        {
            get;
            set;
        }

        LoadingProgressViewModel ConflictsLoadingProgress
        {
            get;
            set;
        }

        string MappedFolder
        {
            get;
        }

        bool Merge(ITfsBranch targetBranch, string pathFilter, IEnumerable<ITfsChangeset> changesets, ITrackProgress trackProgress = null);
        int CheckIn(IEnumerable<ITfsWorkItem> workItemAssociations, string changesetComment);
        void UndoAllPendingChanges();
        void RefreshConflicts();
        void RefreshPendingChanges(CancellationToken cts = default(CancellationToken));
    }
}
