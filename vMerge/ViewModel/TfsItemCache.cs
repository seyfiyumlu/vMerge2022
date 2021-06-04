using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.Managers.Background;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using alexbegh.vMerge.ViewModel.Wrappers;

namespace alexbegh.vMerge.ViewModel
{
    public class ChangesetHasBeenMergedEventArgs : EventArgs
    {
        public TfsChangesetWrapper Changeset { get; private set; }

        public ChangesetHasBeenMergedEventArgs(TfsChangesetWrapper changeset)
        {
            Changeset = changeset;
        }
    }

    public class WorkItemHasBeenMergedEventArgs : EventArgs
    {
        public TfsWorkItemWrapper WorkItem { get; private set; }

        public WorkItemHasBeenMergedEventArgs(TfsWorkItemWrapper workItem)
        {
            WorkItem = workItem;
        }
    }

    public class TfsItemCache
    {
        #region Private Fields
        /// <summary>
        /// The thread id of the thread who created this instance
        /// </summary>
        private int _uiThreadId;
        #endregion

        #region Constructor
        /// <summary>
        /// The constructor
        /// </summary>
        public TfsItemCache()
        {
            _uiThreadId = Thread.CurrentThread.ManagedThreadId;
            Model.Repository.Instance.TfsConnectionInfo.PropertyChanged += TfsConnectionInfo_PropertyChanged;

            _changesetCacheMap = new Dictionary<int, TfsChangesetWrapper>();
            _changesetCache = new ObservableCollection<TfsChangesetWrapper>();

            _workItemCache = new ObservableCollection<TfsWorkItemWrapper>();
            _workItemCacheMap = new Dictionary<int, TfsWorkItemWrapper>();
        }
        #endregion

        #region Work Item Cache
        /// <summary>
        /// Lock object for thread safety
        /// </summary>
        private object _workItemCacheLock = new object();

        /// <summary>
        /// Work Item cache map
        /// </summary>
        private Dictionary<int, TfsWorkItemWrapper> _workItemCacheMap;

        /// <summary>
        /// Cached elements as observablecollection
        /// </summary>
        private ObservableCollection<TfsWorkItemWrapper> _workItemCache;

        /// <summary>
        /// Read-only variant of the cached elements
        /// </summary>
        private ReadOnlyObservableCollection<TfsWorkItemWrapper> _readOnlyWorkItemCache;

        public event EventHandler<WorkItemHasBeenMergedEventArgs> WorkItemHasBeenMerged;

        /// <summary>
        /// The collection of cached elements
        /// </summary>
        public ReadOnlyObservableCollection<TfsWorkItemWrapper> WorkItemCache
        {
            get
            {
                lock (_workItemCacheLock)
                {
                    if (_readOnlyWorkItemCache == null && _workItemCache != null)
                    {
                        _readOnlyWorkItemCache = new ReadOnlyObservableCollection<TfsWorkItemWrapper>(_workItemCache);
                    }
                }
                return _readOnlyWorkItemCache;
            }
        }

        /// <summary>
        /// Checks if a specific element is already in the cache; if so, it updates
        /// the cache (if necessary).
        /// It returns the cached element in any case.
        /// Purpose: avoids reading the elements' properties more than once
        /// (since they involve a round-trip to the TFS server)
        /// </summary>
        /// <param name="workItem">The work item check</param>
        /// <returns>The work item from the cache</returns>
        public TfsWorkItemWrapper UpdateWorkItemFromCache(TfsWorkItemWrapper workItem)
        {
            TfsWorkItemWrapper fromCache = null;

            lock (_workItemCacheLock)
            {
                if (_workItemCacheMap.TryGetValue(workItem.TfsWorkItem.Id, out fromCache))
                {
                    if (fromCache.TfsWorkItem.ChangedDate < workItem.TfsWorkItem.ChangedDate)
                    {
                        fromCache.TfsWorkItem = workItem.TfsWorkItem;
                        return fromCache;
                    }
                    else
                    {
                        return fromCache;
                    }
                }
                _workItemCache.Add(workItem);
                _workItemCacheMap[workItem.TfsWorkItem.Id] = workItem;
            }

            return workItem;
        }
        #endregion

        #region Changeset Item Cache
        /// <summary>
        /// Lock object for thread safety
        /// </summary>
        private object _changesetCacheLock = new object();

        /// <summary>
        /// Work Item cache map
        /// </summary>
        private Dictionary<int, TfsChangesetWrapper> _changesetCacheMap;

        /// <summary>
        /// Cached elements as observablecollection
        /// </summary>
        private ObservableCollection<TfsChangesetWrapper> _changesetCache;

        /// <summary>
        /// Read-only variant of the cached elements
        /// </summary>
        private ReadOnlyObservableCollection<TfsChangesetWrapper> _readOnlyChangesetCache;

        /// <summary>
        /// The collection of cached elements
        /// </summary>
        public ReadOnlyObservableCollection<TfsChangesetWrapper> ChangesetCache
        {
            get
            {
                lock (_changesetCacheLock)
                {
                    if (_readOnlyChangesetCache == null && _changesetCache != null)
                    {
                        _readOnlyChangesetCache = new ReadOnlyObservableCollection<TfsChangesetWrapper>(_changesetCache);
                    }
                }
                return _readOnlyChangesetCache;
            }
        }

        public event EventHandler<ChangesetHasBeenMergedEventArgs> ChangesetHasBeenMerged;

        /// <summary>
        /// Checks if a specific element is already in the cache; if so, it updates
        /// the cache (if necessary).
        /// It returns the cached element in any case.
        /// Purpose: avoids reading the elements' properties more than once
        /// (since they involve a round-trip to the TFS server)
        /// </summary>
        /// <param name="workItem">The work item check</param>
        /// <returns>The work item from the cache</returns>
        public TfsChangesetWrapper UpdateChangesetFromCache(TfsChangesetWrapper changeset)
        {
            TfsChangesetWrapper fromCache = null;

            lock (_changesetCacheLock)
            {
                if (_changesetCacheMap.TryGetValue(changeset.TfsChangeset.Changeset.ChangesetId, out fromCache))
                {
                    if (changeset.TfsChangeset.HasChangesLoaded
                        && fromCache.TfsChangeset.HasChangesLoaded == false)
                    {
                        fromCache.TfsChangeset = changeset.TfsChangeset;
                    }
                    return fromCache;
                }

                _changesetCache.Add(changeset);
                _changesetCacheMap[changeset.TfsChangeset.Changeset.ChangesetId] = changeset;
            }
            return changeset;
        }

        public TfsChangesetWrapper GetChangesetFromCache(int id)
        {
            TfsChangesetWrapper fromCache = null;
            lock (_changesetCacheLock)
            {
                if (_changesetCacheMap.TryGetValue(id, out fromCache))
                {
                    return fromCache;
                }
                return UpdateChangesetFromCache(
                    new TfsChangesetWrapper(Repository.Instance.TfsBridgeProvider.GetChangesetById(id)));
            }
        }

        public IEnumerable<TfsChangesetWrapper> QueryChangesets(ITfsBranch sourceBranch, ITfsBranch targetBranch, string sourcePathFilter = null)
        {
            return 
                Model.Repository.Instance.TfsBridgeProvider
                .GetMergeCandidatesForBranchToBranch(sourceBranch, targetBranch, sourcePathFilter)
                .Select(item => UpdateChangesetFromCache(new TfsChangesetWrapper(item)));
        }

        public IEnumerable<TfsWorkItemWrapper> QueryWorkItems(IEnumerable<TfsChangesetWrapper> changesets, ITrackProgress trackProgress, CancellationToken cancelled = default(CancellationToken))
        {
            var results = new System.Collections.Concurrent.ConcurrentBag<TfsWorkItemWrapper>();

            Parallel.ForEach(changesets,
                (changeset, state) =>
                {
                    if (Repository.Instance.TfsConnectionInfo.Uri == null)
                    {
                        state.Stop();
                        return;
                    }
                    if (cancelled.IsCancellationRequested)
                    {
                        state.Stop();
                        return;
                    }
                    foreach (var workItem in changeset.TfsChangeset.RelatedWorkItems)
                        results.Add(UpdateWorkItemFromCache(new TfsWorkItemWrapper(workItem)));
                    trackProgress.Increment();
                });
            cancelled.ThrowIfCancellationRequested();

            var resultList =
                results
                    .GroupBy(item => item.TfsWorkItem.Id)
                    .Select(item => item.First());

            return resultList.ToList().AsEnumerable();
        }

        public IEnumerable<TfsChangesetWrapper> QueryChangesets(IEnumerable<TfsWorkItemWrapper> workItems, ITrackProgress trackProgress, CancellationToken cancelled = default(CancellationToken))
        {
            var results = new System.Collections.Concurrent.ConcurrentBag<TfsChangesetWrapper>();

            Parallel.ForEach(workItems,
                (workItem, state) =>
                {
                    if (Repository.Instance.TfsConnectionInfo.Uri == null)
                    {
                        state.Stop();
                        return;
                    }

                    if (cancelled.IsCancellationRequested)
                    {
                        state.Stop();
                        return;
                    }
                    trackProgress.Increment();
                    foreach (var changeset in workItem.RelatedChangesets)
                    {
                        trackProgress.ProgressInfo = String.Format("Processing work item #{0} ({1} related changesets)", workItem.TfsWorkItem.Id, workItem.RelatedChangesetCount);
                        results.Add(UpdateChangesetFromCache(new TfsChangesetWrapper(changeset)));
                    }
                });
            cancelled.ThrowIfCancellationRequested();

            var resultList =
                results
                    .GroupBy(item => item.TfsChangeset.Changeset.ChangesetId)
                    .Select(item => item.First());

            return resultList.ToList().AsEnumerable();
        }

        public IEnumerable<TfsWorkItemWrapper> QueryWorkItems(ITfsQuery query)
        {
            foreach (var workItem in query.GetResults())
            {
                yield return UpdateWorkItemFromCache(new TfsWorkItemWrapper(workItem));
            }
        }

        /// <summary>
        /// Highlight work items which are referred to by selected changesets
        /// </summary>
        /// <param name="trackProgressParams">TrackProgressParameters</param>
        public void HighlightWorkItems(TrackProgressParameters trackProgressParams = null)
        {
            bool incrementProgress = false;
            if (trackProgressParams != null)
            {
                if (trackProgressParams.ShouldSetMaximumValue())
                {
                    trackProgressParams.TrackProgress.MaxProgress 
                        = ChangesetCache
                            .Count(changeset => changeset.IsSelected);
                }
                if (!trackProgressParams.ShouldExecute())
                    return;
                incrementProgress = trackProgressParams.ShouldIncrement();
            }

            var workItemsToHighlight = new List<int>();
            var changesetsWithWarnings = new Dictionary<int, string>();
            var map = _workItemCacheMap;
            var warnings = new System.Text.StringBuilder();
            foreach (var item in ChangesetCache.Where(changeset => changeset.IsSelected))
            {
                if (trackProgressParams!=null)
                {
                    if (incrementProgress)
                        trackProgressParams.TrackProgress.Increment();
                    trackProgressParams.CancellationToken.ThrowIfCancellationRequested();
                }

                int countWarnings = 0;
                warnings.Clear();
                item.HasWarning = false;
                foreach (var workItem in item.TfsChangeset.RelatedWorkItems)
                {
                    if (trackProgressParams != null)
                        trackProgressParams.CancellationToken.ThrowIfCancellationRequested();
                    if (map.ContainsKey(workItem.Id))
                    {
                        workItemsToHighlight.Add(workItem.Id);
                    }
                    if (!map.ContainsKey(workItem.Id) || !map[workItem.Id].IsVisible)
                    {
                        ++countWarnings;
                        warnings.Append(String.Format("Work Item #{0}, Title: {1}\n", workItem.Id, workItem.Title));
                    }
                }
                if( countWarnings>0 )
                {
                    changesetsWithWarnings[item.TfsChangeset.Changeset.ChangesetId] = String.Format("The following {0} items were not found in the work item view:\n", countWarnings)
                        + warnings;
                }
            }

            Repository.Instance.BackgroundTaskManager.Send(
                () =>
                {
                    foreach (var item in WorkItemCache)
                    {
                        item.IsHighlighted = workItemsToHighlight.Contains(item.TfsWorkItem.Id);
                    }

                    foreach (var item in ChangesetCache)
                    {
                        string warningText = null;
                        item.HasWarning = changesetsWithWarnings.TryGetValue(item.TfsChangeset.Changeset.ChangesetId, out warningText);
                        item.WarningText = item.HasWarning ? warningText : null;
                    }
                    return true;
                });

        }

        public void ClearHighlightedWorkItems()
        {
            foreach (var workitem in WorkItemCache)
            {
                workitem.IsHighlighted = false;
            }
        }

        public void HighlightChangesets(TrackProgressParameters trackProgressParams = null)
        {
            bool incrementProgress = false;
            if (trackProgressParams != null)
            {
                if (trackProgressParams.ShouldSetMaximumValue())
                {
                    trackProgressParams.TrackProgress.MaxProgress
                        = WorkItemCache
                            .Where(workItem => workItem.IsSelected)
                            .Select(workItem => workItem.RelatedChangesetCount)
                            .Sum();
                }
                if (!trackProgressParams.ShouldExecute())
                    return;
                incrementProgress = trackProgressParams.ShouldIncrement();
            }

            var changesetsToHighlight = new List<int>();
            var workItemsWithWarnings = new Dictionary<int, string>();
            var map = _changesetCacheMap;

            var warnings = new System.Text.StringBuilder();
            foreach (var item in WorkItemCache.Where(workItem => workItem.IsSelected))
            {
                int countWarnings = 0;
                warnings.Clear();
                foreach (var changeset in item.RelatedChangesetsAsEnumerable)
                {
                    if (trackProgressParams != null)
                    {
                        if (incrementProgress)
                            trackProgressParams.TrackProgress.Increment();
                        trackProgressParams.CancellationToken.ThrowIfCancellationRequested();
                    }

                    if (map.ContainsKey(changeset.Changeset.ChangesetId))
                    {
                        changesetsToHighlight.Add(changeset.Changeset.ChangesetId);
                    }
                    if (!map.ContainsKey(changeset.Changeset.ChangesetId) || !map[changeset.Changeset.ChangesetId].IsVisible)
                    {
                        ++countWarnings;
                        warnings.Append(String.Format("Changeset #{0}, Comment: {1}\n", changeset.Changeset.ChangesetId, changeset.Changeset.Comment));
                    }
                }
                if (countWarnings > 0)
                {
                    workItemsWithWarnings[item.TfsWorkItem.Id] =
                        String.Format("The following {0} items were not found in the changeset view:\n", countWarnings)
                        + warnings;
                }
            }

            foreach (var item in ChangesetCache)
            {
                item.IsHighlighted = changesetsToHighlight.Contains(item.TfsChangeset.Changeset.ChangesetId);
            }

            foreach (var item in WorkItemCache)
            {
                string warningText = null;
                item.HasWarning = workItemsWithWarnings.TryGetValue(item.TfsWorkItem.Id, out warningText);
                item.WarningText = item.HasWarning ? warningText : null;
            }

        }

        public void Clear()
        {
            _changesetCache.Clear();
            _workItemCache.Clear();
            _changesetCacheMap.Clear();
            _workItemCacheMap.Clear();
        }

        public void ChangesetsHaveBeenMerged(IEnumerable<ITfsChangeset> changesets)
        {
            if (ChangesetHasBeenMerged == null && WorkItemHasBeenMerged == null)
                return;

            foreach(var changeset in changesets)
            {
                var cachedCS = GetChangesetFromCache(changeset.Changeset.ChangesetId);
                if (ChangesetHasBeenMerged != null)
                    ChangesetHasBeenMerged(this, new ChangesetHasBeenMergedEventArgs(cachedCS));

                //if (WorkItemHasBeenMerged!=null)
                //{
                //    var affectedWIs = WorkItemCache.Where(wi => changeset.RelatedWorkItems.Any(relatedWI => relatedWI.Id == wi.TfsWorkItem.Id));
                //    foreach(var wi in affectedWIs)
                //    {
                //        wi.MergedChangesets.Add(changeset);
                //        foreach(var relatedCS in wi.RelatedChangesets)
                //        {
                //            if (relatedCS.GetAffectedBranchesForActiveProject)
                //        }
                //        if (wi.RelatedChangesets.All(
                //                relatedCS => wi.MergedChangesets.Any(mergedCS => mergedCS.Changeset.ChangesetId == relatedCS.Changeset.ChangesetId)))
                //        {
                //            WorkItemHasBeenMerged(this, new WorkItemHasBeenMergedEventArgs(wi));
                //        }
                //    }
                //}
            }
        }
        #endregion

        #region Private Methods
        void TfsConnectionInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                lock (_workItemCacheLock)
                {
                    lock (_changesetCacheLock)
                    {
                        Model.Repository.Instance.TfsBridgeProvider.ActiveTeamProject = Model.Repository.Instance.TfsConnectionInfo.Project;
                        _workItemCache.Clear();
                        _workItemCacheMap.Clear();
                        _changesetCache.Clear();
                        _changesetCacheMap.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
            }
        }
        #endregion
    }
}
