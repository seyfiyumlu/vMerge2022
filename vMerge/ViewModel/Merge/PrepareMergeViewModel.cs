using Microsoft.TeamFoundation.VersionControl.Client;
using alexbegh.Utility.Commands;
using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.Managers.Background;
using alexbegh.Utility.Managers.View;
using alexbegh.Utility.UserControls.LoadingProgress;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using alexbegh.vMerge.ViewModel.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using alexbegh.Utility.Helpers.Logging;

namespace alexbegh.vMerge.ViewModel.Merge
{
    public class PrepareMergeViewModel : BaseViewModel, IViewModelIsFinishable, ISettingsChangeListener
    {
        #region Inner Classes
        public class ChangesetListElement : NotifyPropertyChangedImpl
        {
            public ChangesetListElement() : base(typeof(ChangesetListElement)) { }

            private bool _originalChangesetLoaded;
            public bool OriginalChangesetLoaded { get { return _originalChangesetLoaded; } set { Set(ref _originalChangesetLoaded, value); } }
            private int _originalChangesetId;
            public int OriginalChangesetId { get { return _originalChangesetId; } set { Set(ref _originalChangesetId, value); } }
            private bool _sourceExists;
            public bool SourceExists { get { return _sourceExists; } set { Set(ref _sourceExists, value); } }
            private ITfsChangeset _sourceChangeset;
            public ITfsChangeset SourceChangeset { get { return _sourceChangeset; } set { Set(ref _sourceChangeset, value); } }
            private string _sourceComment;
            public string SourceComment { get { return _sourceComment; } set { Set(ref _sourceComment, value); } }
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

        // Auto-generated backing property for TfsItemCache
        private TfsItemCache _tfsItemCache;
        #endregion

        #region Private Fields
        /// <summary>
        /// The Tfs item cache
        /// </summary>
        private TfsItemCache TfsItemCache { get { return _tfsItemCache; } set { Set(ref _tfsItemCache, value); } }

        /// <summary>
        /// Lock objects
        /// </summary>
        private object _workItemLock = new object();
        private object _changesetLock = new object();
        #endregion

        #region Static Constructor
        static PrepareMergeViewModel()
        {
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

            MergeSourcesLoading = new LoadingProgressViewModel();
            MergeTargetsLoading = new LoadingProgressViewModel();
            ChangesetsLoading = new LoadingProgressViewModel();
            ChangesetsRefreshing = new LoadingProgressViewModel();

            OpenChangesetCommand = new RelayCommand((o) => OpenChangeset((int)o));
            FindOriginalChangesetCommand = new RelayCommand((o) => FindOriginalChangeset((int)o));
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
                    Changesets = Changesets
                                    .GroupBy(changeset => changeset.Changeset.ChangesetId)
                                    .Select(group => group.First())
                                    .OrderBy(changeset => changeset.Changeset.ChangesetId).ToList();
                });
            if (!finished)
                throw new OperationCanceledException();
            if (!potentialMergeSourceBranches.Any())
                throw new ArgumentException();

            PossibleMergeSources = potentialMergeSourceBranches.ToList();
            ListenToSettingsChanges();
        }

        public PrepareMergeViewModel(TfsItemCache tfsItemCache, IEnumerable<ITfsChangeset> changesets)
            : base(typeof(PrepareMergeViewModel))
        {
            TfsItemCache = tfsItemCache;
            Changesets = changesets.OrderBy(changeset => changeset.Changeset.ChangesetId).ToList();

            MergeSourcesLoading = new LoadingProgressViewModel();
            MergeTargetsLoading = new LoadingProgressViewModel();
            ChangesetsLoading = new LoadingProgressViewModel();
            ChangesetsRefreshing = new LoadingProgressViewModel();

            OpenChangesetCommand = new RelayCommand((o) => OpenChangeset((int)o));
            FindOriginalChangesetCommand = new RelayCommand((o) => FindOriginalChangeset((int)o));
            PerformMergeCommand = new RelayCommand((o) => PerformMerge(o as ChangesetListElement));
            PickPathFilterCommand = new RelayCommand((o) => PickPathFilter());
            AutoMergeCommand = new RelayCommand((o) => AutoMerge(), (o) => IsAnythingToMergeLeft());
            OKCommand = new RelayCommand((o) => OK());


            var potentialMergeSourceBranches = Enumerable.Empty<ITfsBranch>();
            bool finished = Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                (progressParams) =>
                {
                    progressParams.TrackProgress.MaxProgress = Changesets.Count();
                    foreach (var changeset in Changesets)
                    {
                        progressParams.CancellationToken.ThrowIfCancellationRequested();
                        var list = changeset.GetAffectedBranchesForActiveProject().ToArray();
                        potentialMergeSourceBranches = potentialMergeSourceBranches.Union(list);
                        progressParams.TrackProgress.Increment();
                    }
                });
            if (!finished)
                throw new OperationCanceledException();

            PossibleMergeSources = potentialMergeSourceBranches.ToList();
            ListenToSettingsChanges();
        }
        #endregion

        #region Public Properties
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
                return Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.CheckInCommentTemplateKey);
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

        private IReadOnlyList<ChangesetListElement> _changesetList;
        public IReadOnlyList<ChangesetListElement> ChangesetList
        {
            get
            {
                return _changesetList;
            }
            set
            {
                Set(ref _changesetList, value);
            }
        }

        private ITfsBranch _mergeSource;
        public ITfsBranch MergeSource
        {
            get
            {
                return _mergeSource;
            }
            set
            {
                Set(ref _mergeSource, value,
                    () =>
                    {
                        if (!PossibleMergeSources.Contains(value))
                            PossibleMergeSources = PossibleMergeSources.Union(new ITfsBranch[] { value }.AsEnumerable()).ToList();
                        _changesetList = null;
                        if (PathFilter != null)
                        {
                            PathFilter = Repository.Instance.TfsBridgeProvider.GetPathInTargetBranch(MergeSource, PathFilter);
                        }
                        PossibleMergeTargets = Repository.Instance.TfsBridgeProvider.GetPossibleMergeTargetBranches(_mergeSource).ToList();
                    });
            }
        }

        private IReadOnlyList<ITfsBranch> _possibleMergeSources;
        public IReadOnlyList<ITfsBranch> PossibleMergeSources
        {
            get
            {
                return _possibleMergeSources;
            }
            set
            {
                Set(ref _possibleMergeSources, value);
            }
        }

        private IReadOnlyList<ITfsBranch> _possibleMergeTargets;
        public IReadOnlyList<ITfsBranch> PossibleMergeTargets
        {
            get
            {
                return _possibleMergeTargets;
            }
            set
            {
                Set(ref _possibleMergeTargets, value);
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

                Set(ref _mergeTarget, value, () =>
                {
                    LoadAllAssociatedChangesetsIncludingMerges(true);
                });
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

        private bool _nothingToMergeLeft;
        public bool NothingToMergeLeft
        {
            get
            {
                return _nothingToMergeLeft;
            }
            set
            {
                Set(ref _nothingToMergeLeft, value);
            }
        }

        private CheckInSummaryViewModel _embeddedCheckInSummaryViewModel;
        public CheckInSummaryViewModel EmbeddedCheckInSummaryViewModel
        {
            get { return _embeddedCheckInSummaryViewModel; }
            set { Set(ref _embeddedCheckInSummaryViewModel, value); }
        }

        private ITfsTemporaryWorkspace _temporaryWorkspace;

        private RelayCommand _openChangesetCommand;
        public RelayCommand OpenChangesetCommand
        {
            get { return _openChangesetCommand; }
            set { Set(ref _openChangesetCommand, value); }
        }

        private RelayCommand _findOriginalChangesetCommand;
        public RelayCommand FindOriginalChangesetCommand
        {
            get { return _findOriginalChangesetCommand; }
            set { Set(ref _findOriginalChangesetCommand, value); }
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

        private Action _selectNewRowAction;
        public Action SelectNewRowAction
        {
            get { return _selectNewRowAction; }
            set { Set(ref _selectNewRowAction, value); }
        }
        #endregion

        #region Command Handlers
        void OpenChangeset(int id)
        {
            Repository.Instance.TfsUIInteractionProvider.ShowChangeset(id);
        }

        void FindOriginalChangeset(int id)
        {
            var listElement = ChangesetList.Where(item => item.SourceCheckinId == id).FirstOrDefault();
            if (listElement == null)
                throw new ArgumentException("Changeset Id not found in list");

            var changeset = TfsItemCache.GetChangesetFromCache(id);
            if (changeset == null)
                throw new ArgumentException("Changeset not found");

            var sourceChangesets = new Dictionary<int, MergedChangeset>();
            var targetChangesets = new Dictionary<int, MergedChangeset>();

            foreach (var merge in changeset.TfsChangeset.FindMergesForActiveProject(MergeSource, PossibleMergeTargets))
            {
                sourceChangesets[merge.Source.Changeset.ChangesetId] = merge;
                targetChangesets[merge.Target.Changeset.ChangesetId] = merge;
            }

            var relevantId = sourceChangesets.Keys.Except(targetChangesets.Keys).ToArray();
            if (relevantId.Length > 1)
            {
                listElement.OriginalChangesetId = id;
                listElement.OriginalChangesetLoaded = true;
                listElement.HasWarning = true;
                listElement.WarningText = "Could not determine original changeset.";
            }
            else
            {
                listElement.OriginalChangesetId = relevantId.Length > 0 ? relevantId[0] : id;
                listElement.OriginalChangesetLoaded = true;
            }
        }

        void DisplayPathTooLongHelp()
        {
            var mbvm = new MessageBoxViewModel("Local Workspace Path Too Long",
                "TFS ran into the TF14078 error.\r\n" +
                "This means the local workspace path is too long to work with TFS workspace mappings.\r\n" +
                "The only possible workaround is to shorten the path - please check the vMerge Settings in " +
                "the Visual Studio Options page and select a shorter path.", MessageBoxViewModel.MessageBoxButtons.OK);
            Repository.Instance.ViewManager.ShowModal(mbvm);
        }

        void ReportException(Exception ex)
        {
            var mbvm = new MessageBoxViewModel("Exception during Merge occurred",
                "The following exception occurred during the merge operation:\r\n\r\n" +
                ex.ToString(), MessageBoxViewModel.MessageBoxButtons.OK);
            Repository.Instance.ViewManager.ShowModal(mbvm);
        }

        bool PerformMerge(ChangesetListElement item, TrackProgressParameters externalProgress = null)
        {
            SimpleLogger.Checkpoint("PerformMerge: Starting");
            bool returnValue = false;
            bool undoPendingChanges = true;
            if (item == null)
                return false;

            SimpleLogger.Checkpoint("PerformMerge: Getting temporary workspace");
            var tempWorkspace = Repository.Instance.TfsBridgeProvider.GetTemporaryWorkspace(MergeSource, MergeTarget);

            SimpleLogger.Checkpoint("PerformMerge: Undoing pending changes in temporary workspace");
            tempWorkspace.UndoAllPendingChanges();

            try
            {
                var mergeChangeset = TfsItemCache.GetChangesetFromCache(item.SourceCheckinId).TfsChangeset;

                bool finished = false;
                try
                {
                    finished = Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                        (progressParams) =>
                        {
                            tempWorkspace.Merge(
                                MergeTarget, PathFilter,
                                new ITfsChangeset[] { mergeChangeset }.AsEnumerable(),
                                progressParams.TrackProgress);
                        }, String.Format("Merging changeset #{0} ...", item.SourceCheckinId), externalProgress);
                }
                catch (AggregateException ex)
                {
                    bool otherExceptions = false;
                    foreach (var iex in ex.InnerExceptions)
                    {
                        if (iex is LocalPathTooLongException)
                            DisplayPathTooLongHelp();
                        else
                            otherExceptions = true;
                    }
                    if (otherExceptions)
                        SimpleLogger.Log(ex, true);
                }
                catch (LocalPathTooLongException)
                {
                    DisplayPathTooLongHelp();
                    return false;
                }
                catch (Exception ex)
                {
                    SimpleLogger.Log(ex);
                    return false;
                }

                if (!finished)
                {
                    return false;
                }

                SimpleLogger.Checkpoint("PerformMerge: Building check-in summary");
                var checkInSummary = new CheckInSummaryViewModel();
                SimpleLogger.Checkpoint("PerformMerge: Refreshing conflicts");
                tempWorkspace.RefreshConflicts();
                SimpleLogger.Checkpoint("PerformMerge: Refreshing pending changes");
                tempWorkspace.RefreshPendingChanges();
                checkInSummary.SourceBranch = MergeSource;
                checkInSummary.TargetBranch = MergeTarget;
                checkInSummary.Changes =
                    tempWorkspace.PendingChanges
                        .Select(
                            change => new CheckInSummaryViewModel.PendingChangeWithConflict(
                                            change,
                                            tempWorkspace.Conflicts.Where(conflict => conflict.ServerPath == change.ServerPath).FirstOrDefault()))
                        .ToList();

                // Automerging but conflicts?
                if (externalProgress != null && tempWorkspace.Conflicts.Count != 0)
                {
                    return false;
                }

                bool hadConflicts = ResolveConflicts(tempWorkspace, ref finished);

                if (finished)
                {
                    SimpleLogger.Checkpoint("PerformMerge: Finished");
                    if (!item.OriginalChangesetLoaded)
                        FindOriginalChangeset(item.SourceCheckinId);

                    checkInSummary.TemporaryWorkspace = tempWorkspace;

                    var originalCsWrapper = item.OriginalChangesetLoaded ? TfsItemCache.GetChangesetFromCache(item.OriginalChangesetId) : null;
                    var originalCs = originalCsWrapper != null ? originalCsWrapper.TfsChangeset : null;
                    checkInSummary.OriginalChangesets = new ITfsChangeset[] { originalCs ?? mergeChangeset }.ToList();
                    checkInSummary.SourceChangesets = new ITfsChangeset[] { mergeChangeset }.ToList();
                    checkInSummary.AssociatedWorkItems = item.SourceChangeset.RelatedWorkItems;
                    checkInSummary.CheckInComment = BuildCheckInComment(
                        item.SourceChangeset, mergeChangeset);

                    if (hadConflicts || (externalProgress == null && AutoMergeDirectly == false))
                    {
                        SimpleLogger.Checkpoint("PerformMerge: Showing check-in dialog");

                        if (Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.PerformNonModalMergeKey))
                        {
                            undoPendingChanges = false;
                            EmbeddedCheckInSummaryViewModel = checkInSummary;
                            _temporaryWorkspace = tempWorkspace;
                            EmbeddedCheckInSummaryViewModel.Finished += EmbeddedCheckInSummaryViewModel_Finished;
                        }
                        else
                            Repository.Instance.ViewManager.ShowModal(checkInSummary);

                        if (!checkInSummary.Cancelled)
                            CommitMerge(checkInSummary);
                    }
                    else
                    {
                        SimpleLogger.Checkpoint("PerformMerge: Check-in automatically cancelled");
                        checkInSummary.Cancelled = false;
                        CommitMerge(checkInSummary, externalProgress);
                    }
                    returnValue = !hadConflicts;
                }
                else
                {
                    SimpleLogger.Checkpoint("PerformMerge: Not finished");
                }
            }
            finally
            {
                if (undoPendingChanges)
                {
                    SimpleLogger.Checkpoint("PerformMerge: Undoing pending changes");
                    tempWorkspace.UndoAllPendingChanges();
                }
                else
                {
                    SimpleLogger.Checkpoint("PerformMerge: NOT undoing pending changes");
                }
            }
            SimpleLogger.Checkpoint("PerformMerge: Returning");
            return returnValue;
        }

        void EmbeddedCheckInSummaryViewModel_Finished(object sender, ViewModelFinishedEventArgs e)
        {
            if (!EmbeddedCheckInSummaryViewModel.Cancelled)
            {
                CommitMerge(EmbeddedCheckInSummaryViewModel);
            }
            _temporaryWorkspace.UndoAllPendingChanges();
            _temporaryWorkspace = null;
            EmbeddedCheckInSummaryViewModel.Finished -= EmbeddedCheckInSummaryViewModel_Finished;
            EmbeddedCheckInSummaryViewModel = null;
        }

        private static bool ResolveConflicts(ITfsTemporaryWorkspace tempWorkspace, ref bool finished)
        {
            bool hadConflicts = false;
            while (tempWorkspace.Conflicts.Count != 0)
            {
                hadConflicts = true;
                int oldConflictsCount = tempWorkspace.Conflicts.Count;

                SimpleLogger.Checkpoint("PerformMerge: Resolving conflict ({0} remaining)", oldConflictsCount);
                //Repository.Instance.TfsUIInteractionProvider.ResolveConflictsInternally(tempWorkspace);
                //return false;
                Repository.Instance.TfsUIInteractionProvider.ResolveConflictsPerTF(tempWorkspace.MappedFolder);

                SimpleLogger.Checkpoint("PerformMerge: Finished resolving conflict ({0} remaining)", oldConflictsCount);
                tempWorkspace.RefreshConflicts();
                if (tempWorkspace.Conflicts.Count == oldConflictsCount)
                {
                    MessageBoxViewModel mbvm = new MessageBoxViewModel("Cancel merge?", "There are conflicts remaining to be resolved. Really cancel the merge?", MessageBoxViewModel.MessageBoxButtons.None);
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
            return hadConflicts;
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
                Exception ex = null;
                Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                    (progressParams) =>
                    {
                        try
                        {
                            cs = vm.TemporaryWorkspace.CheckIn(associatedWorkItems, vm.CheckInComment);
                            Repository.Instance.BackgroundTaskManager.Post(
                                () =>
                                {
                                    TfsItemCache.ChangesetsHaveBeenMerged(vm.SourceChangesets);
                                    return true;
                                });
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                    }, externalProgress);

                if (ex != null && ex.Message.Contains("TF26006"))
                {
                    MessageBoxViewModel mbvm =
                        new MessageBoxViewModel(
                            "TFS Workspace Exception",
                            "Error TF26006 occurred during check in.\r\n" +
                            "Unfortunately when this happens, the check in has occurred already but\r\n" +
                            "the work item association didn't work; please associate the merged changeset\r\n" +
                            "manually.\r\n\r\n" +
                            "To avoid this error, please clear the local TFS cache manually by \r\n" +
                            "deleting all files in the cache folder before continuing merging.", MessageBoxViewModel.MessageBoxButtons.None);
                    var yesButton = new MessageBoxViewModel.MessageBoxButton("_Show TFS cache");
                    mbvm.ConfirmButtons.Add(yesButton);
                    mbvm.ConfirmButtons.Add(new MessageBoxViewModel.MessageBoxButton("_Ignore"));
                    Repository.Instance.ViewManager.ShowModal(mbvm);
                    if (yesButton.IsChecked)
                    {
                        Process.Start("explorer.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\TeamFoundation\\4.0\\Cache"));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            var newChangeset = TfsItemCache.UpdateChangesetFromCache(new TfsChangesetWrapper(Repository.Instance.TfsBridgeProvider.GetChangesetById(cs)));

            var relatedChangesetListElements =
                _changesetList.Where(element => vm.SourceChangesets.Any(changeset => changeset.Changeset.ChangesetId == element.SourceCheckinId));

            foreach (var element in relatedChangesetListElements)
            {
                element.TargetCheckinDate = newChangeset.TfsChangeset.Changeset.CreationDate;
                element.TargetCheckinId = newChangeset.TfsChangeset.Changeset.ChangesetId;
                element.TargetExists = true;
                element.CanBeMerged = false;
                DetermineWarningStatus(element);
            }

            if (SelectNewRowAction != null)
                SelectNewRowAction();

            NothingToMergeLeft = !IsAnythingToMergeLeft();
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
                ChangesetListElement conflictingChangeset = null;
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
                                conflictingChangeset = changeset;
                                break;
                            }
                        }
                    }, "Merging changesets ...");

                if (conflictingChangeset != null)
                {
                    var comment = conflictingChangeset.SourceComment;
                    if (comment.Length > 80) comment = comment.Substring(0, 77) + "...";
                    var cancelled = new MessageBoxViewModel("vMerge: AutoMerge all changesets", String.Format("AutoMerge has stopped due to a conflict caused by changeset #{0} (comment: \"{1}\").\r\nPlease merge the remaining changesets manually.", conflictingChangeset.SourceCheckinId, comment), MessageBoxViewModel.MessageBoxButtons.OK);
                    Repository.Instance.ViewManager.ShowModal(cancelled);
                }

                LoadAllAssociatedChangesetsIncludingMerges(true);
            }
        }

        public void Close()
        {
            RaiseFinished(false);
        }

        private void OK()
        {
            RaiseFinished(true);
        }
        #endregion

        #region Private Operations
        private void ListenToSettingsChanges()
        {
            Repository.Instance.Settings.AddChangeListener(null, this);
        }

        void ISettingsChangeListener.SettingsChanged(string key, object data)
        {
            if (key == Constants.Settings.CheckInCommentTemplateKey)
                RaisePropertyChanged("CheckInCommentTemplate");
            if (key == Constants.Settings.AutoMergeDirectlyKey)
                RaisePropertyChanged("AutoMergeDirectly");
            if (key == Constants.Settings.LinkMergeWithWorkItemsKey)
                RaisePropertyChanged("LinkMergeWithWorkItems");
        }

        private string BuildCheckInComment(ITfsChangeset original, ITfsChangeset source)
        {
            string prefixText = "";
            string sourceComment = source.Description;
            if (CheckInCommentTemplate.Contains("{PrefixText}"))
            {
                int idx = sourceComment.IndexOf(':');
                if (idx >= 0)
                {
                    prefixText = sourceComment.Substring(0, idx + 1);
                    sourceComment = sourceComment.Substring(idx + 1).Trim();
                }
            }

            return
                CheckInCommentTemplate
                .Replace("{PrefixText}", prefixText)
                .Replace("{OriginalId}", original.Changeset.ChangesetId.ToString())
                .Replace("{OriginalDate}", original.Changeset.CreationDate.ToUniversalTime().ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"))
                .Replace("{OriginalComment}", original.Description)
                .Replace("{SourceId}", source.Changeset.ChangesetId.ToString())
                .Replace("{SourceDate}", source.Changeset.CreationDate.ToUniversalTime().ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"))
                .Replace("{SourceComment}", sourceComment)
                .Replace("{SourceBranch}", MergeSource.Name)
                .Replace("{TargetBranch}", MergeTarget.Name);
        }

        private volatile bool _allAssociatedChangesetsIncludingMergesLoadingTaskActive;
        private void LoadAllAssociatedChangesetsIncludingMerges(bool refresh = false)
        {
            if (_allAssociatedChangesetsIncludingMergesLoadingTaskActive)
            {
                if (!refresh)
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
                = PathFilter == null ? false :
                    sourceCS.TfsChangeset.GetAllChanges()
                        .Any(change => !change.Change.Item.ServerItem.StartsWith(PathFilter, StringComparison.InvariantCultureIgnoreCase));

            bool allChangesNotInFilter
                = PathFilter == null ? false :
                    sourceCS.TfsChangeset.GetAllChanges()
                        .All(change => !change.Change.Item.ServerItem.StartsWith(PathFilter, StringComparison.InvariantCultureIgnoreCase));

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
            var results = new List<ChangesetListElement>();

            lock (_changesetLock)
            {
                int count = 0;

                task.TrackProgress.ProgressInfo = "Determining merge status ...";
                var candidates = Repository.Instance.TfsBridgeProvider.GetMergeCandidatesForBranchToBranch(MergeSource, MergeTarget, PathFilter).ToList();

                count = 0;
                foreach (var changeset in Changesets)
                {
                    task.TrackProgress.ProgressInfo = string.Format("Tracking changesets ({0} done) ...", ++count);
                    MergedChangeset mergedCs;
                    if (candidates.Any(mergecs => mergecs.Changeset.ChangesetId == changeset.Changeset.ChangesetId))
                    {
                        mergedCs = new MergedChangeset()
                        {
                            Source = changeset,
                            Target = null
                        };
                    }
                    else
                    {
                        mergedCs = changeset.FindMergesForActiveProject(MergeSource, (new ITfsBranch[1] { MergeTarget })).FirstOrDefault();

                        if (mergedCs.Source != null && mergedCs.Target.Changeset.ChangesetId == changeset.Changeset.ChangesetId)
                        {
                            var temp = mergedCs.Source;
                            mergedCs.Source = mergedCs.Target;
                            mergedCs.Target = temp;
                        }
                    }

                    if (mergedCs.Source != null)
                    {
                        var csl = new ChangesetListElement()
                        {
                            CanBeMerged = mergedCs.Target == null,
                            SourceChangeset = changeset,
                            SourceComment = changeset.Description,
                            SourceCheckinDate = changeset.Changeset.CreationDate,
                            SourceCheckinId = changeset.Changeset.ChangesetId,
                            SourceExists = true,
                            TargetCheckinDate = (mergedCs.Target != null ? new DateTime?(mergedCs.Target.Changeset.CreationDate) : null),
                            TargetCheckinId = (mergedCs.Target != null ? mergedCs.Target.Changeset.ChangesetId : 0),
                            TargetExists = mergedCs.Target != null
                        };
                        results.Add(csl);
                    }
                }
            }

            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    _allAssociatedChangesetsIncludingMergesLoadingTaskActive = false;
                    ChangesetsRefreshing.IsLoading = false;
                    ChangesetsLoading.IsLoading = false;
                    ChangesetList = results;
                    return true;
                });
        }

        private void FindMergesForChangesetAndBranch(List<MergedChangesetLink> results, ITfsChangeset changeset, ITfsBranch rootBranch)
        {
            var merges
                = changeset.FindMergesForActiveProject(rootBranch, null)
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

        #region Public Operations
        public void SetDefaults()
        {
            if (MergeSource == null && _possibleMergeSources != null && _possibleMergeSources.Count() == 1)
                MergeSource = _possibleMergeSources.First();
        }
        #endregion

        #region Public static helper methods
        public static void ShowNoMergeCandidatesMessageBox(ITfsBranch from, ITfsBranch to)
        {
            var mbvm = new MessageBoxViewModel(
                "No merge candidates",
                String.Format(
                    "There are no pending merges from branch\r\n" +
                    "{0}\r\n" +
                    "to branch\r\n" +
                    "{1}",
                    from.Name, to.Name),
                MessageBoxViewModel.MessageBoxButtons.OK);
            Repository.Instance.ViewManager.ShowModal(mbvm);
        }
        #endregion

        #region Overrides
        protected override void SaveInternal(object data)
        {
            return;
        }
        #endregion

        #region IViewModelIsFinishable

        public event EventHandler<ViewModelFinishedEventArgs> Finished;

        public void RaiseFinished(bool success)
        {
            if (Finished != null)
                Finished(this, new ViewModelFinishedEventArgs(success));
        }
        private void RaisePropertyChanged<TArg>(System.Linq.Expressions.Expression<Func<PrepareMergeViewModel, TArg>> propAccess)
        {
            var expr = propAccess.Body as System.Linq.Expressions.MemberExpression;
            if (expr == null)
                throw new ArgumentException("Not a valid MemberExpression!", "propAccess");

            RaisePropertyChanged(expr.Member.Name);
        }
        #endregion
    }
}
