using Microsoft.TeamFoundation.VersionControl.Client;
using qbusSRL.Utility.Commands;
using qbusSRL.Utility.Helpers.NotifyPropertyChanged;
using qbusSRL.Utility.Helpers.ViewModel;
using qbusSRL.Utility.Managers.Background;
using qbusSRL.Utility.Managers.View;
using qbusSRL.Utility.UserControls.LoadingProgress;
using qbusSRL.vMerge.Model;
using qbusSRL.vMerge.Model.Interfaces;
using qbusSRL.vMerge.ViewModel.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace qbusSRL.vMerge.ViewModel.Merge
{
    public class PrepareMergeViewModel : BaseViewModel, IViewModelIsFinishable
    {
        #region Inner Classes
        public class SourceBranchOption
        {
            public ITfsBranch Branch { get; set; }
            public DateTime CheckinDate { get; set; }
        }

        public class TargetBranchOption
        {
            public ITfsBranch Branch { get; set; }
            public DateTime? MergeDate { get; set; }
        }

        public class ChangesetListElement : NotifyPropertyChangedImpl
        {
            public ChangesetListElement() : base(typeof(ChangesetListElement)) { }

            private bool _sourceExists;
            public bool SourceExists { get { return _sourceExists; } set { Set(ref _sourceExists,value); } }
            private TfsChangesetWrapper _changeset;
            public TfsChangesetWrapper Changeset { get { return _changeset; } set { Set(ref _changeset,value); } }
            private DateTime _sourceCheckinDate;
            public DateTime SourceCheckinDate { get { return _sourceCheckinDate; } set { Set(ref _sourceCheckinDate, value); } }
            private int _sourceCheckinId;
            public int SourceCheckinId { get { return _sourceCheckinId; } set { Set(ref _sourceCheckinId, value); } }
            private bool _targetExists;
            public bool TargetExists { get { return _targetExists; } set { Set(ref _targetExists, value); } }
            private DateTime? _targetCheckinDate;
            public DateTime? TargetCheckinDate { get { return _targetCheckinDate; } set { Set(ref _targetCheckinDate, value); } }
            private int _targetCheckinId;
            public int TargetCheckinId { get { return _targetCheckinId; } set { Set(ref _targetCheckinId, value); } }
            private bool _hasWarning;
            public bool HasWarning { get { return _hasWarning; } set { Set(ref _hasWarning, value); } }
            private string _warningText;
            public string WarningText { get { return _warningText; } set { Set(ref _warningText, value); } }
            private bool _canBeMerged;
            public bool CanBeMerged { get { return _canBeMerged; } set { Set(ref _canBeMerged, value); } }
        }

        public class MergedChangesetLink
        {
            public TfsChangesetWrapper Source { get; set; }
            public TfsChangesetWrapper Target { get; set; }
        }
        #endregion

        #region Private Fields
        /// <summary>
        /// The Tfs item cache
        /// </summary>
        private TfsItemCache TfsItemCache { get; set; }

        /// <summary>
        /// Lock objects
        /// </summary>
        private object _workItemLock = new object();
        private object _changesetLock = new object();

        /// <summary>
        /// The list of branches to consider for tracking
        /// </summary>
        private List<ITfsBranch> _potentialMergeSourceBranches;
        #endregion

        #region Static Constructor
        static PrepareMergeViewModel()
        {
            AddDependency<PrepareMergeViewModel>("AllAssociatedChangesetsIncludingMerges", "MergeSource", "MergeTarget", "ChangesetList", "PossibleMergeSources", "PossibleMergeTargets");
            AddDependency<PrepareMergeViewModel>("MergeSource", "PossibleMergeTargets", "ChangesetList");
            AddDependency<PrepareMergeViewModel>("MergeTarget", "ChangesetList");
            AddDependency<PrepareMergeViewModel>("PathFilter", "ChangesetList");
            AddDependency<PrepareMergeViewModel>("ChangesetList", "AutoMergeCommand");
        }
        #endregion

        #region Constructors
        public PrepareMergeViewModel(TfsItemCache tfsItemCache, IEnumerable<ITfsWorkItem> workItems)
            : base(typeof(PrepareMergeViewModel))
        {
            TfsItemCache = tfsItemCache;
            WorkItems = workItems.ToList();

            MergeSourcesLoading = new LoadingProgressViewModel();
            MergeTargetsLoading = new LoadingProgressViewModel();
            ChangesetsLoading = new LoadingProgressViewModel();

            OpenChangesetCommand = new RelayCommand((o) => OpenChangeset((int)o));
            PerformMergeCommand = new RelayCommand((o) => PerformMerge(o as ChangesetListElement), (o) => o != null);
            PickPathFilterCommand = new RelayCommand((o) => PickPathFilter());
            AutoMergeCommand = new RelayCommand((o) => AutoMerge(), (o) => IsAnythingToMergeLeft());
            OKCommand = new RelayCommand((o) => OK());

            var potentialMergeSourceBranches = Enumerable.Empty<ITfsBranch>();
            bool finished = Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                (progressParams) =>
                {
                    Changesets = new List<ITfsChangeset>();
                    progressParams.TrackProgress.MaxProgress = workItems.Count();
                    foreach (var workItem in workItems)
                    {
                        progressParams.CancellationToken.ThrowIfCancellationRequested();
                        foreach (var changeset in workItem.RelatedChangesets)
                        {
                            progressParams.CancellationToken.ThrowIfCancellationRequested();
                            Changesets.Add(changeset);
                            potentialMergeSourceBranches = potentialMergeSourceBranches.Union(changeset.GetAffectedBranchesForActiveProject());
                        }
                        progressParams.TrackProgress.Increment();
                    }
                });
            if (!finished)
                throw new OperationCanceledException();

            _potentialMergeSourceBranches = potentialMergeSourceBranches.ToList();
        }

        public PrepareMergeViewModel(TfsItemCache tfsItemCache, IEnumerable<ITfsChangeset> changesets)
            : base(typeof(PrepareMergeViewModel))
        {
            TfsItemCache = tfsItemCache;
            Changesets = changesets.Where(changeset => changeset.Changeset.CreationDate >= (DateTime.Now - new TimeSpan(30, 0, 0, 0))).ToList();

            MergeSourcesLoading = new LoadingProgressViewModel();
            MergeTargetsLoading = new LoadingProgressViewModel();
            ChangesetsLoading = new LoadingProgressViewModel();
            ChangesetsRefreshing = new LoadingProgressViewModel();

            OpenChangesetCommand = new RelayCommand((o) => OpenChangeset((int)o));
            PerformMergeCommand = new RelayCommand((o) => PerformMerge(o as ChangesetListElement));
            PickPathFilterCommand = new RelayCommand((o) => PickPathFilter());
            AutoMergeCommand = new RelayCommand((o) => AutoMerge(), (o) => IsAnythingToMergeLeft());
            OKCommand = new RelayCommand((o) => OK());

            var potentialMergeSourceBranches = Enumerable.Empty<ITfsBranch>();
            bool finished = Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                (progressParams) =>
                {
                    WorkItems = new List<ITfsWorkItem>();
                    progressParams.TrackProgress.MaxProgress = Changesets.Count();
                    foreach (var changeset in Changesets)
                    {
                        progressParams.CancellationToken.ThrowIfCancellationRequested();
                        foreach (var workItem in changeset.RelatedWorkItems)
                        {
                            progressParams.CancellationToken.ThrowIfCancellationRequested();
                            WorkItems.Add(workItem);
                        }
                        potentialMergeSourceBranches = potentialMergeSourceBranches.Union(changeset.GetAffectedBranchesForActiveProject());
                        progressParams.TrackProgress.Increment();
                    }
                });
            if (!finished)
                throw new OperationCanceledException();

            _potentialMergeSourceBranches = potentialMergeSourceBranches.ToList();
        }
        #endregion

        #region Public Properties
        private List<ITfsWorkItem> _workItems;
        public List<ITfsWorkItem> WorkItems
        {
            get { return _workItems; }
            private set { Set(ref _workItems, value); }
        }

        private List<ITfsChangeset> _changesets;
        public List<ITfsChangeset> Changesets
        {
            get { return _changesets; }
            private set { Set(ref _changesets, value, () => { _changesetList = null; }); }
        }

        private LoadingProgressViewModel _mergeSourcesLoading;
        public LoadingProgressViewModel MergeSourcesLoading
        {
            get { return _mergeSourcesLoading; }
            private set { Set(ref _mergeSourcesLoading, value); }
        }

        private LoadingProgressViewModel _mergeTargetsLoading;
        public LoadingProgressViewModel MergeTargetsLoading
        {
            get { return _mergeTargetsLoading; }
            private set { Set(ref _mergeTargetsLoading, value); }
        }

        private LoadingProgressViewModel _changesetsLoading;
        public LoadingProgressViewModel ChangesetsLoading
        {
            get { return _changesetsLoading; }
            private set { Set(ref _changesetsLoading, value); }
        }

        private LoadingProgressViewModel _changesetsRefreshing;
        public LoadingProgressViewModel ChangesetsRefreshing
        {
            get { return _changesetsRefreshing; }
            private set { Set(ref _changesetsRefreshing, value); }
        }

        public bool AutoMergeDirectly
        {
            get
            {
                return Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.AutoMergeDirectlyKey);
            }
            set
            {
                if (AutoMergeDirectly != value)
                {
                    Repository.Instance.Settings.SetSettings(Constants.Settings.AutoMergeDirectlyKey, value);
                    RaisePropertyChanged();
                }
            }
        }

        public bool ShowConfirmationDialog
        {
            get
            {
                return Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.ShowConfirmationDialogKey);
            }
            set
            {
                if (ShowConfirmationDialog != value)
                {
                    Repository.Instance.Settings.SetSettings(Constants.Settings.ShowConfirmationDialogKey, value);
                    RaisePropertyChanged();
                }
            }
        }

        public bool LinkMergeWithWorkItems
        {
            get
            {
                return Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.LinkMergeWithWorkItemsKey);
            }
            set
            {
                if (LinkMergeWithWorkItems != value)
                {
                    Repository.Instance.Settings.SetSettings(Constants.Settings.LinkMergeWithWorkItemsKey, value);
                    RaisePropertyChanged();
                }
            }
        }

        public string CheckInCommentTemplate
        {
            get
            {
                return Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.CheckInCommentTemplateKey)
                    ?? "vMerge: Merged {SourceId} from {SourceBranch}";
            }
            set
            {
                if (CheckInCommentTemplate != value)
                {
                    Repository.Instance.Settings.SetSettings(Constants.Settings.CheckInCommentTemplateKey, value);
                    RaisePropertyChanged();
                }
            }
        }

        private List<ChangesetListElement> _changesetList;
        public IReadOnlyList<ChangesetListElement> ChangesetList
        {
            get
            {
                if (_changesets == null)
                    return null;

                if (AllAssociatedChangesetsIncludingMerges == null)
                    return null;

                if (_changesetList == null)
                {
                    LoadChangesetList();
                }
                return _changesetList;
            }
        }

        private ITfsBranch _mergeSource;
        public ITfsBranch MergeSource
        {
            get
            {
                if (_mergeSource == null)
                {
                    if (AllAssociatedChangesetsIncludingMerges == null)
                        return null;

                    int maxChangesetId = AllAssociatedChangesetsIncludingMerges.Max(link => link.Target.TfsChangeset.Changeset.ChangesetId);
                    var mostRecentChangeset =
                        AllAssociatedChangesetsIncludingMerges
                        .Where(link => link.Target.TfsChangeset.Changeset.ChangesetId == maxChangesetId)
                        .Select(link => link.Target)
                        .FirstOrDefault();

                    _mergeSource =
                        (mostRecentChangeset != null)
                        ? mostRecentChangeset.TfsChangeset.GetAffectedBranchesForActiveProject().FirstOrDefault()
                        : null;
                }
                return _mergeSource;
            }
            set
            {
                var oldValue = _mergeSource;
                Set(ref _mergeSource, value, 
                    () => 
                    {
                        _changesetList = null;
                        if (PathFilter != null)
                        {
                            PathFilter = Repository.Instance.TfsBridgeProvider.GetPathInTargetBranch(MergeSource, PathFilter);
                            //PathFilter = PathFilter.Replace(oldValue.Name, MergeSource.Name);
                        }
                    });
            }
        }

        private List<SourceBranchOption> _possibleMergeSources;
        public IReadOnlyList<SourceBranchOption> PossibleMergeSources
        {
            get
            {
                if (_possibleMergeSources != null)
                    return _possibleMergeSources;

                if (AllAssociatedChangesetsIncludingMerges == null)
                {
                    MergeSourcesLoading.IsLoading = true;
                    MergeSourcesLoading.ProgressInfo = "Loading ...";
                    return null;
                }

                MergeSourcesLoading.IsLoading = false;
                var unsortedResults
                    = AllAssociatedChangesetsIncludingMerges
                        .SelectMany(link => link.Target.TfsChangeset.GetAffectedBranchesForActiveProject())
                        .Distinct()
                        .OrderBy(item => item.Name)
                        .Select(item => new SourceBranchOption() { Branch = item }).ToList();

                foreach (var branch in unsortedResults)
                {
                    branch.CheckinDate
                        = AllAssociatedChangesetsIncludingMerges
                            .Where(link => link.Target.TfsChangeset.GetAffectedBranchesForActiveProject()
                                                    .Contains(branch.Branch))
                            .Select(link => link.Target.TfsChangeset.Changeset.CreationDate)
                            .FirstOrDefault();
                }

                _possibleMergeSources
                    = unsortedResults.OrderBy(item => item.CheckinDate).ToList();
                return _possibleMergeSources;
            }
        }

        private Dictionary<ITfsBranch, List<TargetBranchOption>> _possibleMergeTargets;
        public IReadOnlyList<TargetBranchOption> PossibleMergeTargets
        {
            get
            {
                if (AllAssociatedChangesetsIncludingMerges == null)
                {
                    MergeTargetsLoading.IsLoading = true;
                    MergeTargetsLoading.ProgressInfo = "Loading ...";
                    return null;
                }
                MergeTargetsLoading.IsLoading = false;

                if (MergeSource == null)
                    return null;

                if (_possibleMergeTargets == null)
                {
                    _possibleMergeTargets = new Dictionary<ITfsBranch, List<TargetBranchOption>>();
                }

                List<TargetBranchOption> result;
                if (_possibleMergeTargets.TryGetValue(MergeSource, out result))
                {
                    return result;
                }

                var possibleBranches
                    = Repository.Instance.TfsBridgeProvider.GetPossibleMergeTargetBranches(MergeSource);

                var unsortedResults
                    = possibleBranches
                        .Select(item => new TargetBranchOption() { Branch = item }).ToList();

                foreach (var branch in unsortedResults)
                {
                    branch.MergeDate
                        = AllAssociatedChangesetsIncludingMerges
                            .Where(link => link.Target.TfsChangeset.GetAffectedBranchesForActiveProject()
                                                    .Contains(branch.Branch))
                            .Select(link => link.Target.TfsChangeset.Changeset.CreationDate)
                            .FirstOrDefault();
                }

                _possibleMergeTargets[MergeSource]
                    = unsortedResults.OrderBy(item => item.MergeDate).ToList();
                return _possibleMergeTargets[MergeSource];
            }
        }

        private ITfsBranch _mergeTarget;
        public ITfsBranch MergeTarget
        {
            get
            {
                return _mergeTarget;
            }
            set
            {
                if (value == null)
                    return;

                Set(ref _mergeTarget, value, () => { _changesetList = null; });
            }
        }

        private string _pathFilter;
        public string PathFilter
        {
            get
            {
                return _pathFilter;
            }
            set
            {
                Set(ref _pathFilter, value, () => { _changesetList = null; });
            }
        }

        private List<MergedChangesetLink> _allAssociatedChangesetsIncludingMerges;
        public IReadOnlyList<MergedChangesetLink> AllAssociatedChangesetsIncludingMerges
        {
            get
            {
                if (_allAssociatedChangesetsIncludingMerges != null)
                    return _allAssociatedChangesetsIncludingMerges;

                LoadAllAssociatedChangesetsIncludingMerges();
                return _allAssociatedChangesetsIncludingMerges;
            }
        }

        private RelayCommand _openChangesetCommand;
        public RelayCommand OpenChangesetCommand
        {
            get { return _openChangesetCommand; }
            set { Set(ref _openChangesetCommand, value); }
        }

        private RelayCommand _performMergeCommand;
        public RelayCommand PerformMergeCommand
        {
            get { return _performMergeCommand; }
            set { Set(ref _performMergeCommand, value); }
        }

        private RelayCommand _pickPathFilterCommand;
        public RelayCommand PickPathFilterCommand
        {
            get { return _pickPathFilterCommand; }
            set { Set(ref _pickPathFilterCommand, value); }
        }

        private RelayCommand _autoMergeCommand;
        public RelayCommand AutoMergeCommand
        {
            get { return _autoMergeCommand; }
            set { Set(ref _autoMergeCommand, value); }
        }

        private RelayCommand _okCommand;
        public RelayCommand OKCommand
        {
            get { return _okCommand; }
            set { Set(ref _okCommand, value); }
        }

        public event EventHandler Finished;
        #endregion

        #region Command Handlers
        void OpenChangeset(int id)
        {
            Repository.Instance.TfsUIInteractionProvider.ShowChangeset(id);
        }

        bool PerformMerge(ChangesetListElement item, TrackProgressParameters externalProgress = null)
        {
            bool returnValue = false;
            if (item == null)
                return false;

            var tempWorkspace = Repository.Instance.TfsBridgeProvider.GetTemporaryWorkspace(MergeSource, MergeTarget);
            var mergeChangeset = TfsItemCache.GetChangesetFromCache(item.SourceCheckinId).TfsChangeset;

            bool finished = Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                (progressParams) =>
                {
                    tempWorkspace.Merge(
                        MergeTarget, PathFilter,
                        new ITfsChangeset[] { mergeChangeset }.AsEnumerable(), 
                        progressParams.TrackProgress);
                }, String.Format("Merging changeset #{0} ...", item.SourceCheckinId), externalProgress);

            if (!finished)
            {
                tempWorkspace.UndoAllPendingChanges();
                return false;
            }

            var checkInSummary = new CheckInSummaryViewModel();
            tempWorkspace.RefreshConflicts();
            checkInSummary.Changes =
                tempWorkspace.PendingChanges
                    .Select(
                        change => new CheckInSummaryViewModel.PendingChangeWithConflict(
                                        change, 
                                        tempWorkspace.Conflicts.Where(conflict => conflict.ServerPath == change.ServerPath).FirstOrDefault()))
                    .ToList();

            while (tempWorkspace.Conflicts.Count != 0)
            {
                int oldConflictsCount = tempWorkspace.Conflicts.Count;
                Repository.Instance.TfsUIInteractionProvider.ResolveConflictsPerTF(tempWorkspace.MappedFolder);
                tempWorkspace.RefreshConflicts();
                if (tempWorkspace.Conflicts.Count == oldConflictsCount)
                {
                    MessageBoxViewModel mbvm = new MessageBoxViewModel("Cancel merge?", "There are conflicts remaining to be solved. Really cancel the merge?", MessageBoxViewModel.MessageBoxButtons.None);
                    var yesButton = new MessageBoxViewModel.MessageBoxButton("_Yes");
                    mbvm.ConfirmButtons.Add(yesButton);
                    mbvm.ConfirmButtons.Add(new MessageBoxViewModel.MessageBoxButton("_No"));
                    Repository.Instance.ViewManager.ShowModal(mbvm);

                    if (yesButton.IsChecked)
                    {
                        finished = false;
                        break;
                    }
                }
            }

            if (finished)
            {
                checkInSummary.TemporaryWorkspace = tempWorkspace;
                checkInSummary.OriginalChangesets = new ITfsChangeset[] { item.Changeset.TfsChangeset }.ToList();
                checkInSummary.SourceChangesets = new ITfsChangeset[] { mergeChangeset }.ToList();
                checkInSummary.AssociatedWorkItems = item.Changeset.TfsChangeset.RelatedWorkItems;
                checkInSummary.CheckInComment = BuildCheckInComment(
                    item.Changeset.TfsChangeset, mergeChangeset);

                if (externalProgress==null)
                {
                    var view = Repository.Instance.ViewManager.CreateViewFor(checkInSummary);
                    view.Finished += CheckInDialogClosed;
                }
                else
                {
                    checkInSummary.Cancelled = false;
                    CommitMerge(checkInSummary, externalProgress);
                }
                returnValue = true;
            }
            else
            {
                tempWorkspace.UndoAllPendingChanges();
            }
            return returnValue;
        }

        void PickPathFilter()
        {
            var result = Repository.Instance.TfsUIInteractionProvider.BrowseForTfsFolder(PathFilter);
            if (result != null)
            {
                PathFilter = result;
            }
        }

        private void CommitMerge(CheckInSummaryViewModel vm, TrackProgressParameters externalProgress = null)
        {
            var associatedWorkItems
                = vm
                    .SourceChangesets
                    .SelectMany(changeset => changeset.RelatedWorkItems)
                    .GroupBy(workItem => workItem.WorkItem.Id)
                    .Select(group => group.First());

            int cs = -1;

            try
            {
                Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                    (progressParams) =>
                    {
                        //cs = vm.TemporaryWorkspace.CheckIn(associatedWorkItems, vm.CheckInComment);
                        cs = 1;
                    }, externalProgress);
            }
            catch (OperationCanceledException)
            {
            }

            var newChangeset = TfsItemCache.UpdateChangesetFromCache(new TfsChangesetWrapper(Repository.Instance.TfsBridgeProvider.GetChangesetById(cs)));

            var relatedChangesetListElements =
                _changesetList.Where(element => vm.SourceChangesets.Any(changeset => changeset.Changeset.ChangesetId == element.SourceCheckinId));

            foreach (var sourceCS in vm.SourceChangesets)
                _allAssociatedChangesetsIncludingMerges.Add(new MergedChangesetLink() { Source = TfsItemCache.GetChangesetFromCache(sourceCS.Changeset.ChangesetId), Target = newChangeset });

            foreach (var element in relatedChangesetListElements)
            {
                element.TargetCheckinDate = newChangeset.TfsChangeset.Changeset.CreationDate;
                element.TargetCheckinId = newChangeset.TfsChangeset.Changeset.ChangesetId;
                element.TargetExists = true;
                element.CanBeMerged = false;
                DetermineWarningStatus(element);
            }
        }
        
        private void CheckInDialogClosed(object sender, EventArgs e)
        {
            var vm = (CheckInSummaryViewModel)sender;
            if (!vm.Cancelled)
            {
                CommitMerge(vm);
            }
        }

        private bool IsAnythingToMergeLeft()
        {
            return ChangesetList != null && ChangesetList.Any(changeset => changeset.CanBeMerged);
        }

        private void AutoMerge()
        {
            var confirm = new MessageBoxViewModel("vMerge: AutoMerge all changesets", "All changesets will be merged silently as long as no conflict arises. Are you really sure to proceed?", MessageBoxViewModel.MessageBoxButtons.None);
            var goOn = new MessageBoxViewModel.MessageBoxButton("Merge silently");
            var cancel = new MessageBoxViewModel.MessageBoxButton("Cancel");

            confirm.ConfirmButtons.Add(goOn);
            confirm.ConfirmButtons.Add(cancel);
            Repository.Instance.ViewManager.ShowModal(confirm);
            if (goOn.IsChecked)
            {
                Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                    (progressParams) =>
                    {
                        progressParams.TrackProgress.MaxProgress = ChangesetList.Where(item => item.CanBeMerged).Count();
                        foreach (var changeset in ChangesetList.Where(item => item.CanBeMerged).OrderBy(item => item.SourceCheckinId))
                        {
                            progressParams.TrackProgress.Increment();
                            progressParams.CancellationToken.ThrowIfCancellationRequested();

                            var copy = progressParams.CloneWithoutIncrements();
                            if (!PerformMerge(changeset, copy))
                            {
                                var cancelled = new MessageBoxViewModel("vMerge: AutoMerge all changesets", "AutoMerge has been cancelled. Please merge the remaining changesets manually.", MessageBoxViewModel.MessageBoxButtons.OK);
                                Repository.Instance.ViewManager.ShowModal(cancelled);
                            }
                        }
                    }, "Merging changesets ...");

                LoadAllAssociatedChangesetsIncludingMerges(true);
            }
        }

        private void OK()
        {
            if (Finished != null)
                Finished(this, new EventArgs());
        }
        #endregion

        #region Private Operations
        private string BuildCheckInComment(ITfsChangeset original, ITfsChangeset source)
        {
            return
                CheckInCommentTemplate
                .Replace("{OriginalId}", original.Changeset.ChangesetId.ToString())
                .Replace("{OriginalDate}", original.Changeset.CreationDate.ToUniversalTime().ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"))
                .Replace("{OriginalComment}", original.Description)
                .Replace("{SourceId}", source.Changeset.ChangesetId.ToString())
                .Replace("{SourceDate}", source.Changeset.CreationDate.ToUniversalTime().ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"))
                .Replace("{SourceComment}", source.Description)
                .Replace("{SourceBranch}", MergeSource.Name)
                .Replace("{TargetBranch}", MergeTarget.Name);
        }

        private ITfsChangeset FindChangeset(ITfsChangeset source, ITfsBranch where)
        {
            if (source.GetAffectedBranchesForActiveProject().Contains(where))
                return source;


            var foundLinks
                = AllAssociatedChangesetsIncludingMerges.Where(link => link.Source != null && link.Source.TfsChangeset.Changeset.ChangesetId == source.Changeset.ChangesetId);

            // This could be more optimized.
            foreach (var foundLink in foundLinks)
            {
                if (foundLink.Target.TfsChangeset.GetAffectedBranchesForActiveProject().Contains(where))
                    return foundLink.Target.TfsChangeset;
            }
            foreach (var foundLink in foundLinks)
            {
                var found = FindChangeset(foundLink.Target.TfsChangeset, where);
                if (found != null)
                    return found;
            }
            return null;
        }

        private volatile bool _changesetLoadingTaskActive;
        private void LoadChangesetList(bool refresh = false)
        {
            if (_changesetLoadingTaskActive)
            {
                if (!refresh)
                    return;
            }

            _changesetLoadingTaskActive = true;
            Repository.Instance.BackgroundTaskManager.Start(
                Constants.Tasks.LoadChangesetListKey,
                refresh ? ChangesetsRefreshing : ChangesetsLoading,
                (task) =>
                {
                    var results = new List<ChangesetListElement>();
                    task.TrackProgress.ProgressInfo = "Loading changesets ...";
                    lock (_changesetLock)
                    {
                        int count = 0;
                        foreach (var changeset in _changesets)
                        {
                            task.TrackProgress.ProgressInfo = string.Format("Loading changesets ({0} done) ...", ++count);
                            var source = FindChangeset(changeset, MergeSource);
                            var target = FindChangeset(changeset, MergeTarget);

                            var changesetListElement =
                                new ChangesetListElement()
                                {
                                    Changeset = TfsItemCache.UpdateChangesetFromCache(new TfsChangesetWrapper(changeset)),
                                    CanBeMerged = (target == null),

                                    SourceExists = (source != null),
                                    SourceCheckinDate = (source != null) ? source.Changeset.CreationDate : DateTime.MinValue,
                                    SourceCheckinId = (source != null) ? source.Changeset.ChangesetId : 0,

                                    TargetExists = (target != null),
                                    TargetCheckinDate = (target != null) ? new DateTime?(target.Changeset.CreationDate) : null,
                                    TargetCheckinId = (target != null) ? target.Changeset.ChangesetId : 0
                                };
                            DetermineWarningStatus(changesetListElement);
                            results.Add(changesetListElement);
                        }
                    }
                    Repository.Instance.BackgroundTaskManager.Post(
                        () =>
                        {
                            _changesetLoadingTaskActive = false;
                            _changesetList = results;
                            RaisePropertyChanged("ChangesetList");
                            return true;
                        });
                });
        }

        private volatile bool _allAssociatedChangesetsIncludingMergesLoadingTaskActive;
        private void LoadAllAssociatedChangesetsIncludingMerges(bool refresh = false)
        {
            if (_allAssociatedChangesetsIncludingMergesLoadingTaskActive)
            {
                if( !refresh )
                    return;
            }

            LoadingProgressViewModel lpvm = refresh ? ChangesetsRefreshing : ChangesetsLoading;
            _allAssociatedChangesetsIncludingMergesLoadingTaskActive = true;
            Repository.Instance.BackgroundTaskManager.Start(
                Constants.Tasks.LoadAllAssociatedChangesetsIncludingMergesKey,
                lpvm,
                LoadAllAssociatedChangesetsIncludingMergesTask);
        }

        private bool ChangesetConsistsOnlyOfMerges(ITfsChangeset changeset)
        {
            return changeset
                    .GetAllChanges()
                    .All(change => change.Change.ChangeType.HasFlag(ChangeType.Merge));
        }

        private bool ChangesetDoesNotContainMerges(ITfsChangeset changeset)
        {
            return changeset
                    .GetAllChanges()
                    .Any(change => !change.Change.ChangeType.HasFlag(ChangeType.Merge));
        }

        private bool ChangesetHasMixedContent(ITfsChangeset changeset)
        {
            return changeset
                    .GetAllChanges()
                    .Any(change => change.Change.ChangeType.HasFlag(ChangeType.Merge))
                    &&
                    changeset
                    .GetAllChanges()
                    .Any(change => !change.Change.ChangeType.HasFlag(ChangeType.Merge));
        }

        private void DetermineWarningStatus(ChangesetListElement changesetListElement)
        {
            if (changesetListElement.TargetCheckinId != 0)
                return;

            if (changesetListElement.SourceCheckinId == 0)
            {
                changesetListElement.HasWarning = true;
                changesetListElement.WarningText =
                    "This changeset has not yet been merged to the selected source branch.";
                changesetListElement.CanBeMerged = false;
                return;
            }

            TfsChangesetWrapper sourceCS =
                TfsItemCache.GetChangesetFromCache(changesetListElement.SourceCheckinId);

            bool anyChangeNotInFilter
                = PathFilter==null ? false :
                    sourceCS.TfsChangeset.GetAllChanges()
                        .Any(change => !change.Change.Item.ServerItem.StartsWith(PathFilter));

            bool allChangesNotInFilter
                = PathFilter==null ? false :
                    sourceCS.TfsChangeset.GetAllChanges()
                        .All(change => !change.Change.Item.ServerItem.StartsWith(PathFilter));

            if (anyChangeNotInFilter || allChangesNotInFilter)
            {
                changesetListElement.HasWarning = true;
                if (allChangesNotInFilter)
                {
                    changesetListElement.WarningText =
                        "No change in this changeset is part of the merge due to the path filter selection.";
                    changesetListElement.CanBeMerged = false;
                }
                else if (anyChangeNotInFilter)
                {
                    changesetListElement.WarningText =
                        "Some changes in this changeset are not part of the merge due to the path filter selection.";
                }
            }
        }

        private void LoadAllAssociatedChangesetsIncludingMergesTask(BackgroundTask task)
        {
            task.TrackProgress.ProgressInfo = "Loading ...";
            var results = new List<MergedChangesetLink>();

            lock (_changesetLock)
            {
                bool fallback = false;
                int count = 0;

                foreach(var changeset in Changesets)
                {
                    task.TrackProgress.ProgressInfo = string.Format("Loading changesets ({0} done)", ++count);
                    if (ChangesetHasMixedContent(changeset))
                    {
                        fallback = true;
                    }
                }

                if( !fallback )
                {
                    var rootChangesets = new List<ITfsChangeset>();
                    count = 0;
                    foreach (
                        var changesetWithMerges 
                        in Changesets.Where(
                                changeset => ChangesetConsistsOnlyOfMerges(changeset)))
                    {
                        task.TrackProgress.ProgressInfo = string.Format("Finding root changesets ({0} done) ...", ++count);
                        var rootChangesetResult = changesetWithMerges.SourceChangesets.Where(changeset => ChangesetDoesNotContainMerges(changeset)).ToArray();
                        if (rootChangesetResult.Length != 1)
                        {
                            fallback = true;
                            break;
                        }
                        rootChangesets.Add(rootChangesetResult[0]);

                    }

                    if (!fallback)
                    {
                        Changesets.RemoveAll(
                            changeset => ChangesetConsistsOnlyOfMerges(changeset));
                        foreach (var rootCS in rootChangesets)
                        {
                            if (Changesets.Any(cs => cs.Changeset.ChangesetId == rootCS.Changeset.ChangesetId))
                                continue;
                            Changesets.Add(rootCS);
                        }
                    }
                }

                var temporaryRootBranches =
                    Changesets
                        .SelectMany(changeset => changeset.GetAffectedBranchesForActiveProject())
                        .GroupBy(branch => branch.Name)
                        .OrderByDescending(grouping => grouping.Count())
                        .Select(grouping => grouping.First())
                        .ToList();

                count = 0;
                foreach (var changeset in Changesets)
                {
                    task.TrackProgress.ProgressInfo = string.Format("Tracking changesets ({0} done) ...", ++count);
                    foreach (var rootBranch in temporaryRootBranches)
                    {
                        FindMergesForChangesetAndBranch(results, changeset, rootBranch);
                    }
                }
            }

            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    _allAssociatedChangesetsIncludingMergesLoadingTaskActive = false;
                    _allAssociatedChangesetsIncludingMerges = results;
                    bool loadChangesets = _changesetList != null;
                    RaisePropertyChanged("AllAssociatedChangesetsIncludingMerges");
                    if (loadChangesets)
                        LoadChangesetList(true);
                    return true;
                });
        }

        private void FindMergesForChangesetAndBranch(List<MergedChangesetLink> results, ITfsChangeset changeset, ITfsBranch rootBranch)
        {
            var merges
                = changeset.FindMergesForActiveProject(rootBranch, _potentialMergeSourceBranches)
                  .Select(
                    item => new MergedChangesetLink()
                    {
                        Source = TfsItemCache.UpdateChangesetFromCache(new TfsChangesetWrapper(item.Source)),
                        Target = TfsItemCache.UpdateChangesetFromCache(new TfsChangesetWrapper(item.Target))
                    });

            results.Add(
                new MergedChangesetLink()
                {
                    Source = null,
                    Target = TfsItemCache.UpdateChangesetFromCache(new TfsChangesetWrapper(changeset))
                });
            results.AddRange(merges);
        }
        #endregion

        #region Overrides
        protected override void SaveInternal(object data)
        {
            return;
        }
        #endregion
    }
}
