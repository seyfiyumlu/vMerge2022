using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alexbegh.Utility.Managers.Background;
using System.Collections.ObjectModel;
using System.Threading;
using alexbegh.Utility.UserControls.LoadingProgress;
using alexbegh.Utility.Helpers.Logging;

namespace alexbegh.vMerge.Model.Implementation
{
    class TfsTemporaryWorkspace : ITfsTemporaryWorkspace
    {
        public Workspace TfsWorkspace
        {
            get;
            internal set;
        }

        public ITfsBranch SourceBranch
        {
            get;
            internal set;
        }

        public string MappedFolder
        {
            get;
            private set;
        }

        private TfsBridgeProvider TfsBridgeProvider
        {
            get;
            set;
        }

        public IReadOnlyList<ITfsMergeConflict> Conflicts
        {
            get;
            set;
        }

        public string TargetFolder
        {
            get;
            private set;
        }

        public IReadOnlyList<ITfsPendingChange> PendingChanges
        {
            get;
            private set;
        }

        public LoadingProgressViewModel PendingChangesLoadingProgress
        {
            get;
            set;
        }

        public LoadingProgressViewModel ConflictsLoadingProgress
        {
            get;
            set;
        }

        private event EventHandler _pendingChangesChanged;
        public event EventHandler PendingChangesChanged
        {
            add { _pendingChangesChanged += value; }
            remove { _pendingChangesChanged -= value; }
        }

        private event EventHandler _conflictsChanged;
        public event EventHandler ConflictsChanged
        {
            add { _conflictsChanged += value; }
            remove { _conflictsChanged -= value; }
        }

        private bool _conflictsRefreshPending;

        internal TfsTemporaryWorkspace(TfsBridgeProvider host, Workspace tfsWorkspace, ITfsBranch sourceBranch, string mappedFolder, string targetFolder)
        {
            TfsBridgeProvider = host;
            SourceBranch = sourceBranch;
            MappedFolder = mappedFolder;
            TargetFolder = targetFolder;
            Conflicts = new List<ITfsMergeConflict>();
            PendingChanges = new List<ITfsPendingChange>();

            TfsWorkspace = tfsWorkspace;

            //TfsWorkspace.VersionControlServer.ResolvedConflict += VersionControlServer_ResolvedConflict;
            //TfsWorkspace.VersionControlServer.Conflict += VersionControlServer_Conflict;

            //TfsWorkspace.VersionControlServer.NewPendingChange += VersionControlServer_NewPendingChange;
            //TfsWorkspace.VersionControlServer.PendingChangeCandidatesChanged += VersionControlServer_PendingChangeCandidatesChanged;
            //TfsWorkspace.VersionControlServer.PendingChangesChanged += VersionControlServer_PendingChangesChanged;
        }

        void RaisePendingChangesChanged()
        {
            if (_pendingChangesChanged != null)
                _pendingChangesChanged(this, new EventArgs());
        }

        void RaiseConflictsChanged()
        {
            if (_conflictsChanged != null)
                _conflictsChanged(this, new EventArgs());
        }

        void VersionControlServer_Conflict(object sender, ConflictEventArgs e)
        {
            RefreshConflictsAsync();
        }

        private void VersionControlServer_ResolvedConflict(object sender, ResolvedConflictEventArgs e)
        {
            RefreshConflictsAsync();
        }

        void VersionControlServer_PendingChangesChanged(object sender, WorkspaceEventArgs e)
        {
            RefreshPendingChanges();
        }

        private void VersionControlServer_NewPendingChange(object sender, PendingChangeEventArgs e)
        {
            RefreshPendingChanges();
        }

        void VersionControlServer_PendingChangeCandidatesChanged(object sender, WorkspaceEventArgs e)
        {
            RefreshPendingChanges();
        }

        public bool Merge(ITfsBranch targetBranch, string pathFilter, IEnumerable<ITfsChangeset> changesetsAsEnumerable, ITrackProgress trackProgress = null)
        {
            SimpleLogger.Checkpoint("Merge: {0}, {1}", targetBranch != null ? targetBranch.Name : null, pathFilter);
            if (pathFilter != null && !SourceBranch.Name.StartsWith(pathFilter, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("If a PathFilter is provided, it needs to be below the SourceBranch");

            var changesets = new List<ITfsChangeset>(changesetsAsEnumerable);
            var files = new List<string>();

            if (trackProgress != null)
            {
                trackProgress.MaxProgress = changesets.Count * 2 + 2;
                trackProgress.CurrentProgress = 0;
                trackProgress.ProgressInfo = "Acquiring file list ...";
            }
                        
            foreach (var changeset in changesets)
            {
                SimpleLogger.Checkpoint("Merge: Processing changeset #{0}", changeset != null ? changeset.Changeset.ChangesetId : -1);
                foreach (var change in changeset.Changes.Select(c => c.Change))
                {
                    SimpleLogger.Checkpoint("Merge: change {0}, filter {1}", change.Item.ServerItem, pathFilter);
                    if (pathFilter != null && change.Item.ServerItem.StartsWith(pathFilter, StringComparison.InvariantCultureIgnoreCase) == false)
                        continue;

                    files.Add(change.Item.ServerItem);
                }
                ++trackProgress.CurrentProgress;
            }

            GetRequest[] getRequests = 
                files.Distinct().Select(file => new GetRequest(file, RecursionType.None, VersionSpec.Latest)).ToArray();

            if (trackProgress != null)
            {
                trackProgress.ProgressInfo = "Getting files ...";
                ++trackProgress.CurrentProgress;
            }

            SimpleLogger.Checkpoint("Merge: Getting files");
            GetStatus gs = TfsWorkspace.Get(getRequests, GetOptions.None);

            if (trackProgress != null)
            {
                trackProgress.ProgressInfo = "Performing merges ...";
                ++trackProgress.CurrentProgress;
            }

            var resultingConflicts = new List<ITfsMergeConflict>();
            foreach (var changeset in changesets)
            {
                if (trackProgress != null)
                {
                    ++trackProgress.CurrentProgress;
                }
                var csvs = new ChangesetVersionSpec(changeset.Changeset.ChangesetId);

                SimpleLogger.Checkpoint("Merge: Performing merge on CS# {0}", changeset != null ? changeset.Changeset.ChangesetId : -1);
                var mergeResult = TfsWorkspace.Merge(
                    pathFilter ?? SourceBranch.Name,
                    targetBranch.Name,
                    csvs, csvs, LockLevel.None, RecursionType.Full, MergeOptions.ForceMerge);

                if (mergeResult.NumFailures > 0)
                {
                    foreach (var failure in mergeResult.GetFailures())
                    {
                        if (failure.Code == "TF14078" || failure.Message.Contains("TF14078"))
                        {
                            throw new LocalPathTooLongException(failure.Message);
                        }
                        throw new InvalidOperationException(failure.Message);
                    }
                }
            }

            if (trackProgress != null)
            {
                trackProgress.CurrentProgress = trackProgress.MaxProgress;
            }

            SimpleLogger.Checkpoint("Merge: Refreshing conflicts");
            RefreshConflictsWorker(default(CancellationToken));

            SimpleLogger.Checkpoint("Merge: Finished");
            return Conflicts.Count != 0;
        }

        void RefreshPendingChangesWorker(CancellationToken cts)
        {
            var pendingChanges = new List<ITfsPendingChange>();
            var pendingChangesTfs = TfsWorkspace.GetPendingChangesEnumerable();
            foreach (var pendingChange in pendingChangesTfs)
            {
                cts.ThrowIfCancellationRequested();
                pendingChanges.Add(new TfsPendingChange(pendingChange));
            }

            PendingChanges = pendingChanges;
            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    RaisePendingChangesChanged();
                    return true;
                });
        }

        public void RefreshPendingChanges(CancellationToken cts = default(CancellationToken))
        {
            RefreshPendingChangesWorker(cts);
        }

        public void RefreshConflictsWorker(CancellationToken cts = default(CancellationToken))
        {
            string[] conflictPaths = new string[1];
            conflictPaths[0] = TargetFolder;
                // targetBranch.Name;

            var result = new List<ITfsMergeConflict>();
            Conflict[] conflicts = TfsWorkspace.QueryConflicts(conflictPaths, true);
            if (conflicts.Length > 0)
            {
                foreach (var conflict in conflicts)
                {
                    bool resolved = false;
                    cts.ThrowIfCancellationRequested();
                    if (TfsWorkspace.MergeContent(conflict, false))
                    {
                        if (conflict.ContentMergeSummary.TotalConflicting == 0)
                        {
                            conflict.Resolution = Resolution.AcceptMerge;

                            try
                            {
                                TfsWorkspace.ResolveConflict(conflict);
                            }
                            catch (Exception ex)
                            {
                                // Ignore exception "This conflict was not found on the server. Another user might have already resolved this conflict."
                                if (!ex.Message.Contains("TF10167"))
                                    throw ex;
                                resolved = true;
                            }
                            if (!resolved)
                                resolved = conflict.IsResolved;
                        }
                    }
                    if (!resolved)
                    {
                        result.Add(new TfsMergeConflict(conflict));
                    }
                }
            }

            Conflicts = result;
            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    RaiseConflictsChanged();
                    return true;
                });
        }

        void RefreshConflictsAsync()
        {
            if (_conflictsRefreshPending)
                return;

            _conflictsRefreshPending = true;
            Repository.Instance.BackgroundTaskManager.DelayedPost(
                () =>
                {
                    _conflictsRefreshPending= false;
                    Repository.Instance.BackgroundTaskManager.Start(
                        Constants.Tasks.ConflictsRefreshTaskKey,
                        ConflictsLoadingProgress,
                        (task) =>
                        {
                            if (task.TrackProgress != null)
                                task.TrackProgress.ProgressInfo = "Refreshing conflicts ...";
                            RefreshConflictsWorker(task.Cancelled.Token);
                        });
                    return true;
                });
        }

        public void RefreshConflicts()
        {
            Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                (progressParams) =>
                {
                    RefreshConflictsWorker(default(CancellationToken));
                });
        }

        public int CheckIn(IEnumerable<ITfsWorkItem> workItemAssociations, string changesetComment)
        {
            return TfsWorkspace.CheckIn(
                TfsWorkspace.GetPendingChangesEnumerable().ToArray(),
                changesetComment, null,
                workItemAssociations.Select(
                    wia => new WorkItemCheckinInfo(Repository.Instance.TfsBridgeProvider.GetWorkItemById(wia.Id).WorkItem, WorkItemCheckinAction.Associate)).ToArray(),
                null);
        }

        public void UndoAllPendingChanges()
        {
            TfsWorkspace.Undo("$/", RecursionType.Full);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            Repository.Instance.BackgroundTaskManager.Cancel(Constants.Tasks.PendingChangeRefreshTaskKey);
            Repository.Instance.BackgroundTaskManager.Cancel(Constants.Tasks.ConflictsRefreshTaskKey);

            if (TfsWorkspace != null)
            {
                //TfsWorkspace.VersionControlServer.ResolvedConflict -= VersionControlServer_ResolvedConflict;
                //TfsWorkspace.VersionControlServer.Conflict -= VersionControlServer_Conflict;

                //TfsWorkspace.VersionControlServer.NewPendingChange -= VersionControlServer_NewPendingChange;
                //TfsWorkspace.VersionControlServer.PendingChangeCandidatesChanged -= VersionControlServer_PendingChangeCandidatesChanged;
                //TfsWorkspace.VersionControlServer.PendingChangesChanged -= VersionControlServer_PendingChangesChanged;

                var tfsWorkspace = TfsWorkspace;
                TfsWorkspace = null;
                TfsBridgeProvider.DeleteTemporaryWorkspace(tfsWorkspace, MappedFolder);
            }
        }
    }
}
