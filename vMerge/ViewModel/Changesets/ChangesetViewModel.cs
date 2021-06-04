using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.UserControls.FieldMapperGrid;
using alexbegh.Utility.UserControls.LoadingProgress;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.ViewModel.Wrappers;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using alexbegh.vMerge.Model.Interfaces;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using alexbegh.Utility.Commands;
using alexbegh.vMerge.ViewModel.Configuration;
using System.Collections.Generic;
using alexbegh.Utility.Managers.Background;
using alexbegh.vMerge.ViewModel.ViewSelection;
using alexbegh.vMerge.StudioIntegration.Framework;
using alexbegh.vMerge.ViewModel.Merge;
using System.Text.RegularExpressions;
using alexbegh.vMerge.ViewModel.Profile;
using alexbegh.Utility.Helpers.Logging;
using System.Windows.Threading;
using alexbegh.vMerge.StudioIntegration;

namespace alexbegh.vMerge.ViewModel.Changesets
{
    class ChangesetViewModel : TfsListBaseViewModel
    {
        #region Private Methods
        private void AttachToChangesetCache()
        {
            ((INotifyCollectionChanged)TfsItemCache.ChangesetCache).CollectionChanged +=
                (c, a) =>
                {
                    if (a.NewItems != null)
                    {
                        foreach (var item in a.NewItems.Cast<TfsChangesetWrapper>())
                        {
                            item.PropertyChanged += ChangesetPropertyChanged;
                        }
                    }

                    if (a.OldItems != null)
                    {
                        foreach (var item in a.OldItems.Cast<TfsChangesetWrapper>())
                        {
                            item.PropertyChanged -= ChangesetPropertyChanged;
                        }
                    }
                };

            foreach (var item in TfsItemCache.ChangesetCache)
            {
                item.PropertyChanged += ChangesetPropertyChanged;
            }

            TfsItemCache.ChangesetHasBeenMerged += TfsItemCache_ChangesetHasBeenMerged;
        }

        private void TfsItemCache_ChangesetHasBeenMerged(object sender, ChangesetHasBeenMergedEventArgs e)
        {
            if (ViewSelectionViewModel.IsMergeCandidatesSelected())
            {
                var original = ChangesetList.OriginalElements.Where(changeset => changeset == e.Changeset).FirstOrDefault();
                if (original != null)
                    ChangesetList.OriginalElements.Remove(original);

                var source = ChangesetList.Elements.Where(changeset => changeset.InnerItem == e.Changeset).FirstOrDefault();
                if (source != null)
                    ChangesetList.Elements.Remove(source);
            }
        }

        protected override IEnumerable<FieldMapperGridColumn> GetColumns()
        {
            var data = Repository.Instance.Settings.FetchSettings<ObservableCollection<FieldMapperGridColumn>>("ChangesetViewModel.ColumnSettings");
            if (data != null)
                foreach (var column in data)
                    yield return column;
            else
            {
                yield return new FieldMapperGridColumn() { Column = "IsSelected", Header = "IsSelected", Visible = true, Width = 100, Type = typeof(bool) };
                yield return new FieldMapperGridColumn() { Column = "ChangesetId", Header = "ChangesetId", Visible = true, Width = 70, Type = typeof(int) };
                yield return new FieldMapperGridColumn() { Column = "CreationDate", Header = "CreationDate", Visible = true, Width = 100, Type = typeof(DateTime) };
                yield return new FieldMapperGridColumn() { Column = "OwnerDisplayName", Header = "User", Visible = true, Width = 100, Type = typeof(string) };
                yield return new FieldMapperGridColumn() { Column = "Comment", Header = "Comment", Visible = true, Width = 200, Type = typeof(string) };
            }
        }

        protected override void UpdateColumns(ObservableCollection<FieldMapperGridColumn> columns)
        {
            ChangesetList.SetColumns(columns);
            Columns = columns;
        }

        private void ResetChangesets()
        {
            Changesets.Clear();
            FilteredChangesets.Clear();
        }

        private void LoadChangesets()
        {
            if (Repository.Instance.TfsConnectionInfo.Uri == null)
            {
                ResetChangesets();
                return;
            }

            if (ViewSelectionViewModel.IsMergeCandidatesSelected())
            {
                var result = ViewSelectionViewModel.GetMergeSourceTargetBranches();
                ResetChangesets();

                Repository.Instance.BackgroundTaskManager.Start(
                    Constants.Tasks.LoadChangesetsTaskKey,
                    ItemsLoading,
                    (task) =>
                    {
                        LoadChangesets(result.SourceBranch, result.TargetBranch, result.PathFilter, task);
                    });
            }
            else if (ViewSelectionViewModel.IsQuerySelected())
            {
                var query = ViewSelectionViewModel.GetSelectedQuery();
                ResetChangesets();

                Repository.Instance.BackgroundTaskManager.Start(
                    Constants.Tasks.LoadChangesetsTaskKey,
                    ItemsLoading,
                    (task) =>
                    {
                        LoadChangesets(query, task);
                    });
            }
            else
            {
                ItemsLoading.IsLoading = false;
            }
        }

        private void SetContent(ObservableCollection<TfsChangesetWrapper> changesets)
        {
            if (Repository.Instance.TfsConnectionInfo.Uri == null)
            {
                ResetChangesets();
                return;
            }

            Changesets = changesets;
            ApplyFilter();
        }

        private void LoadChangesets(ITfsBranch sourceBranch, ITfsBranch targetBranch, string sourcePathFilter, BackgroundTask task)
        {
            task.TrackProgress.ProgressInfo = "Loading merge candidates ...";
            var changesets = new ObservableCollection<TfsChangesetWrapper>(TfsItemCache.QueryChangesets(sourceBranch, targetBranch, sourcePathFilter));

            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    if (!task.Cancelled.IsCancellationRequested)
                        SetContent(changesets);
                    return true;
                });
        }

        private void LoadChangesets(ITfsQuery query, BackgroundTask task)
        {
            task.TrackProgress.ProgressInfo = "Loading work items ...";
            var workItems = new List<TfsWorkItemWrapper>(
                TfsItemCache.QueryWorkItems(query));
            task.TrackProgress.ProgressInfo = "Loading changesets ...";
            task.TrackProgress.MaxProgress = workItems.Count;
            var changesets
                = new ObservableCollection<TfsChangesetWrapper>(
                    TfsItemCache.QueryChangesets(workItems, task.TrackProgress, task.Cancelled.Token));
            
            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    if (!task.Cancelled.IsCancellationRequested)
                        SetContent(changesets);
                    return true;
                });
        }

        private void HighlightWorkItems()
        {
            _changesetHighlightUpdatePending = false;
            if (!Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                (trackProgressParameters) =>
                {
                    TfsItemCache.HighlightWorkItems(trackProgressParameters);
                }))
            {
                SuppressNotifications();
                try
                {
                    foreach (var item in _changesetsToResetOnCancel)
                    {
                        item.Item1.IsSelected = item.Item2;
                    }
                }
                finally
                {
                    AllowNotifications();
                }
            }
            _changesetsToResetOnCancel.Clear();
        }

        private void DataWasModified()
        {
            if (IsDirty)
                return;
            IsDirty = true;
            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    SaveInternal(Repository.Instance.Settings);
                    return true;
                });
        }

        private void AttachToProfileProvider()
        {
            Repository.Instance.ProfileProvider.DefaultProfileChanged += ProfileProvider_DefaultProfileChanged;
        }

        private void DetachFromProfileProvider()
        {
            Repository.Instance.ProfileProvider.DefaultProfileChanged -= ProfileProvider_DefaultProfileChanged;
        }
        #endregion

        #region Static Constructor
        static ChangesetViewModel()
        {
            AddDependency<ChangesetViewModel>("Enabled", "ShowPendingMergeInfo", "LoadMergeProfilesEnabled");
            AddDependency<ChangesetViewModel>("MergeProfiles", "LoadMergeProfilesEnabled");
        }
        #endregion

        #region Constructor
        public ChangesetViewModel(TfsItemCache tfsItemCache)
            : base("Changeset", typeof(ChangesetViewModel))
        {
            Repository.Instance.VMergeUIProvider.MergeWindowVisibilityChanged += (o, a) => Enabled = !Repository.Instance.VMergeUIProvider.IsMergeWindowVisible();
            Enabled = !Repository.Instance.VMergeUIProvider.IsMergeWindowVisible();
            _applyFilterTimer = new DispatcherTimer();
            _applyFilterTimer.Interval = new TimeSpan(0,0,0,0,350);
            _applyFilterTimer.Tick += (o, a) => PerformApplyFilter();

            TfsItemCache = tfsItemCache;

            ViewSelectionViewModel = new ViewSelectionViewModel();
            Changesets = new ObservableCollection<TfsChangesetWrapper>();
            FilteredChangesets = new ObservableCollection<TfsChangesetWrapper>();
            _changesetsToResetOnCancel = new List<Tuple<TfsChangesetWrapper, bool>>();
            ChangesetList =
                new FieldMapperGridViewModel<TfsChangesetWrapper>(
                    new ChangesetPropertyAccessor(),
                    FilteredChangesets,
                    Columns);
            ChangesetList.ColumnSettingsChanged += (o, a) => DataWasModified();

            ViewSelectionViewModel.ViewSelectionChanged += OnViewSelectionChanged;

            ItemSelectedCommand = new RelayCommand((o) => ItemSelected(o), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            ShowWorkItemViewCommand = new RelayCommand((o) => ShowWorkItemView(), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            SelectMarkedItemsCommand = new RelayCommand((o) => SelectMarkedItems(), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            ViewChangesetCommand = new RelayCommand((o) => ViewChangeset(o), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            TrackChangesetCommand = new RelayCommand((o) => TrackChangeset(o), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            MergeCommand = new RelayCommand((o) => Merge(o), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            ShowMergeViewCommand = new RelayCommand((o) => ShowMergeView(), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            SelectAllCommand = new RelayCommand((o) => SelectAll(), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            SaveMergeProfileCommand = new RelayCommand((o) => SaveMergeProfile(), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);
            RefreshCommand = new RelayCommand((o) => Refresh(), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);

            AttachToChangesetCache();
            AttachToProfileProvider();
            Repository.Instance.TfsBridgeProvider.ActiveProjectSelected += (o, a) =>
                {
                    Changesets.Clear();
                    FilteredChangesets.Clear();
                    if (Repository.Instance.TfsBridgeProvider.ActiveTeamProject!=null)
                        Repository.Instance.BackgroundTaskManager.DelayedPost(() => { ProfileProvider_DefaultProfileChanged(null, null); return true; });
                };
        }
        #endregion

        #region Private Event Handlers
        private void ChangesetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SuppressNotificationsCounter > 0)
                return;

            if (e.PropertyName == "IsSelected")
            {
                try
                {
                    SuppressNotifications();
                    var senderChangeset = (sender as TfsChangesetWrapper);
                    foreach (var other in ChangesetList.SelectedElements)
                    {
                        _changesetsToResetOnCancel.Add(new Tuple<TfsChangesetWrapper, bool>(other, other.IsSelected));
                        other.IsSelected = senderChangeset.IsSelected;
                    }
                }
                finally
                {
                    AllowNotifications();
                }

                _changesetsToResetOnCancel.Add(new Tuple<TfsChangesetWrapper, bool>(sender as TfsChangesetWrapper, !(sender as TfsChangesetWrapper).IsSelected));
                if (!_changesetHighlightUpdatePending)
                {
                    _changesetHighlightUpdatePending = true;
                    Repository.Instance.BackgroundTaskManager.Post(
                        () =>
                        {
                            HighlightWorkItems();
                            return true;
                        });
                }
            }
        }

        private void OnViewSelectionChanged(object sender, EventArgs e)
        {
            LoadChangesets();
            ViewSelectionViewModel.ShowViewOptions = false;
            var defaultProfile = Repository.Instance.ProfileProvider.GetDefaultProfile();

            try
            {
                DetachFromProfileProvider();
                if (ViewSelectionViewModel.IsMergeCandidatesSelected())
                {
                    var result = ViewSelectionViewModel.GetMergeSourceTargetBranches();
                    defaultProfile.CSQueryName = null;
                    defaultProfile.CSSourceBranch = result.SourceBranch.Name;
                    defaultProfile.CSTargetBranch = result.TargetBranch.Name;
                    defaultProfile.CSSourcePathFilter = result.PathFilter;
                }
                else if (ViewSelectionViewModel.IsQuerySelected())
                {
                    var query = ViewSelectionViewModel.GetSelectedQuery();
                    defaultProfile.CSSourceBranch = null;
                    defaultProfile.CSTargetBranch = null;
                    defaultProfile.CSSourcePathFilter = null;
                    defaultProfile.CSQueryName = query.QualifiedTitle;
                }
            }
            finally
            {
                AttachToProfileProvider();
            }
        }

        void ProfileProvider_DefaultProfileChanged(object sender, DefaultProfileChangedEventArgs e)
        {
            var defaultProfile = Repository.Instance.ProfileProvider.GetDefaultProfile();

            if (defaultProfile == null)
            {
                Repository.Instance.BackgroundTaskManager.DelayedPost(() => { ProfileProvider_DefaultProfileChanged(sender, e); return false; });
                return;
            }
                
            bool didSomething = false;
            if (defaultProfile.CSQueryName != null)
            {
                didSomething = true;
                if (ViewSelectionViewModel.RootQuery == null)
                {
                    Repository.Instance.BackgroundTaskManager.DelayedPost(
                        () =>
                        {
                            ProfileProvider_DefaultProfileChanged(sender, e);
                            return true;
                        });
                }
                else
                {
                    ViewSelectionViewModel.ViewType = ViewSelection.ViewSelectionViewModel.ViewTypeEnum.WorkItemQuery;
                    ViewSelectionViewModel.SelectedQuery
                        = ViewSelectionViewModel.RootQuery.All.Where(query => query.QualifiedTitle == defaultProfile.CSQueryName).FirstOrDefault() as ITfsQuery;
                    if (ViewSelectionViewModel.SelectedQuery==null)
                    {
                        defaultProfile.CSQueryName = null;
                        didSomething = false;
                    }
                }
            }
            if (defaultProfile.CSSourceBranch != null)
            {
                didSomething = true;
                if (ViewSelectionViewModel.AvailableSourceBranches == null)
                {
                    Repository.Instance.BackgroundTaskManager.DelayedPost(
                        () =>
                        {
                            ProfileProvider_DefaultProfileChanged(sender, e);
                            return true;
                        });
                }
                else
                {
                    ViewSelectionViewModel.ViewType = ViewSelection.ViewSelectionViewModel.ViewTypeEnum.MergeCandidates;
                    var sourceBranch = Repository.Instance.TfsBridgeProvider.GetBranchByNameOrNull(defaultProfile.CSSourceBranch);
                    if (sourceBranch == null)
                    {
                        ViewSelectionViewModel.SourceBranch =
                        ViewSelectionViewModel.AvailableSourceBranches.Where(branch => branch.Name == defaultProfile.CSSourceBranch).FirstOrDefault();
                        if (ViewSelectionViewModel.TargetBranches != null)
                        {
                            ViewSelectionViewModel.TargetBranch =
                                ViewSelectionViewModel.TargetBranches.Where(branch => branch.Name == defaultProfile.CSTargetBranch).FirstOrDefault();
                        }
                    }
                    else
                    {
                        ViewSelectionViewModel.SourceBranch = sourceBranch;
                        ViewSelectionViewModel.TargetBranch = defaultProfile.CSTargetBranch != null ? Repository.Instance.TfsBridgeProvider.GetBranchByNameOrNull(defaultProfile.CSTargetBranch) : null;
                    }
                    ViewSelectionViewModel.PathFilter = defaultProfile.CSSourcePathFilter;
                    
                    if (ViewSelectionViewModel.SourceBranch == null)
                    {
                        defaultProfile.CSSourceBranch = null;
                        defaultProfile.CSTargetBranch = null;
                        didSomething = false;
                    }
                    if (ViewSelectionViewModel.TargetBranch==null)
                    {
                        defaultProfile.CSTargetBranch = null;
                        didSomething = false;
                    }
                }
            }

            IncludeCommentFilter = defaultProfile.ChangesetIncludeCommentFilter;
            IncludeCommentFilterActive = IncludeCommentFilter != null;
            ExcludeCommentFilter = defaultProfile.ChangesetExcludeCommentFilter;
            ExcludeCommentFilterActive = ExcludeCommentFilter != null;
            DateFromFilter = defaultProfile.DateFromFilter;
            DateFromFilterActive = DateFromFilter != null;
            DateToFilter = defaultProfile.DateToFilter;
            DateToFilterActive = DateToFilter != null;
            IncludeUserFilter = defaultProfile.ChangesetIncludeUserFilter;
            IncludeByUserFilterActive = IncludeUserFilter != null;


            if (!didSomething)
            {
                ItemsLoading.IsLoading = false;
            }
        }
        #endregion

        #region Private Fields/Properties
        private bool _changesetHighlightUpdatePending;
        private List<Tuple<TfsChangesetWrapper, bool>> _changesetsToResetOnCancel;

        private TfsItemCache _tfsItemCache;
        private TfsItemCache TfsItemCache
        {
            get { return _tfsItemCache; }
            set { _tfsItemCache = value; }
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }
        #endregion

        #region Properties
        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            private set { Set(ref _enabled, value); }
        }

        public bool LoadMergeProfilesEnabled
        {
            get
            {
                return Enabled && MergeProfiles != null && MergeProfiles.Any();
            }
        }

        public bool ShowPendingMergeInfo
        {
            get { return !_enabled; }
        }

        private ViewSelectionViewModel _viewSelectionViewModel;
        public ViewSelectionViewModel ViewSelectionViewModel
        {
            get { return _viewSelectionViewModel; }
            set { Set(ref _viewSelectionViewModel, value); }
        }

        private ObservableCollection<TfsChangesetWrapper> _changesets;
        public ObservableCollection<TfsChangesetWrapper> Changesets
        {
            get { return _changesets; }
            set
            {
                Set(ref _changesets, value);
            }
        }

        private ObservableCollection<TfsChangesetWrapper> _filteredChangesets;
        public ObservableCollection<TfsChangesetWrapper> FilteredChangesets
        {
            get { return _filteredChangesets; }
            set { Set(ref _filteredChangesets, value); }
        }

        private FieldMapperGridViewModel<TfsChangesetWrapper> _changesetList;
        public FieldMapperGridViewModel<TfsChangesetWrapper> ChangesetList
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

        private bool _showChangesetFilter;
        public bool ShowChangesetFilter
        {
            get { return _showChangesetFilter; }
            set { Set(ref _showChangesetFilter, value); }
        }

        private bool _includeCommentFilterActive;
        public bool IncludeCommentFilterActive
        {
            get { return _includeCommentFilterActive; }
            set { Set(ref _includeCommentFilterActive, value, ApplyFilter); }
        }

        private string _includeCommentFilter;
        public string IncludeCommentFilter
        {
            get { return _includeCommentFilter; }
            set { Set(ref _includeCommentFilter, value, ApplyFilter); }
        }

        private bool _excludeCommentFilterActive;
        public bool ExcludeCommentFilterActive
        {
            get { return _excludeCommentFilterActive; }
            set { Set(ref _excludeCommentFilterActive, value, ApplyFilter); }
        }

        private string _excludeCommentFilter;
        public string ExcludeCommentFilter
        {
            get { return _excludeCommentFilter; }
            set { Set(ref _excludeCommentFilter, value, ApplyFilter); }
        }

        private bool _includeByUserFilterActive;
        public bool IncludeByUserFilterActive
        {
            get { return _includeByUserFilterActive; }
            set { Set(ref _includeByUserFilterActive, value, ApplyFilter); }
        }

        private string _includeUserFilter;
        public string IncludeUserFilter
        {
            get { return _includeUserFilter; }
            set { Set(ref _includeUserFilter, value, ApplyFilter); }
        }

        private bool _dateFromFilterActive;
        public bool DateFromFilterActive
        {
            get { return _dateFromFilterActive; }
            set { Set(ref _dateFromFilterActive, value, ApplyFilter); }
        }

        private DateTime? _dateFromFilter;
        public DateTime? DateFromFilter
        {
            get { return _dateFromFilter; }
            set { Set(ref _dateFromFilter, value, ApplyFilter); }
        }

        private bool _dateToFilterActive;
        public bool DateToFilterActive
        {
            get { return _dateToFilterActive; }
            set { Set(ref _dateToFilterActive, value, ApplyFilter); }
        }

        private DateTime? _dateToFilter;
        public DateTime? DateToFilter
        {
            get { return _dateToFilter; }
            set { Set(ref _dateToFilter, value, ApplyFilter); }
        }

        private DispatcherTimer _applyFilterTimer;
        private DateTime _lastApplyFilterTimerTick;
        #endregion

        #region Command Properties
        private RelayCommand _itemSelectedCommand;
        private RelayCommand ItemSelectedCommand
        {
            get { return _itemSelectedCommand; }
            set { Set(ref _itemSelectedCommand, value); }
        }

        private RelayCommand _showWorkItemViewCommand;
        public RelayCommand ShowWorkItemViewCommand
        {
            get { return _showWorkItemViewCommand; }
            set { Set(ref _showWorkItemViewCommand, value); }
        }

        private RelayCommand _selectMarkedItemsCommand;
        public RelayCommand SelectMarkedItemsCommand
        {
            get { return _selectMarkedItemsCommand; }
            set { Set(ref _selectMarkedItemsCommand, value); }
        }

        private RelayCommand _viewChangesetCommand;
        public RelayCommand ViewChangesetCommand
        {
            get { return _viewChangesetCommand; }
            set { Set(ref _viewChangesetCommand, value); }
        }

        private RelayCommand _trackChangesetCommand;
        public RelayCommand TrackChangesetCommand
        {
            get { return _trackChangesetCommand; }
            set { Set(ref _trackChangesetCommand, value); }
        }

        private RelayCommand _mergeCommand;
        public RelayCommand MergeCommand
        {
            get { return _mergeCommand; }
            set { Set(ref _mergeCommand, value); }
        }

        private RelayCommand _showMergeViewCommand;
        public RelayCommand ShowMergeViewCommand
        {
            get { return _showMergeViewCommand; }
            set { Set(ref _showMergeViewCommand, value); }
        }

        private RelayCommand _selectAllCommand;
        public RelayCommand SelectAllCommand
        {
            get { return _selectAllCommand; }
            set { Set(ref _selectAllCommand, value); }
        }

        private RelayCommand _saveMergeProfileCommand;
        public RelayCommand SaveMergeProfileCommand
        {
            get { return _saveMergeProfileCommand; }
            set { Set(ref _saveMergeProfileCommand, value); }
        }
        #endregion

        #region Command Handlers
        void ItemSelected(object o)
        {
        }

        void SelectAll()
        {
            foreach (var item in FilteredChangesets)
            {
                item.IsSelected = true;
            }
        }

        void SaveMergeProfile()
        {
            var vm = new SaveProfileAsViewModel();
            var dlg = Repository.Instance.ViewManager.ShowModal(vm);
        }

        protected override void Refresh()
        {
            base.Refresh();
            ItemsLoading.IsLoading = true;
            ItemsLoading.ProgressInfo = "Reloading projects, branches and changesets ... please wait";
            ResetChangesets();
            SetContent(new ObservableCollection<TfsChangesetWrapper>());
            TfsItemCache.Clear();
            Repository.Instance.TfsBridgeProvider.Refresh();
        }

        void SelectMarkedItems()
        {
            try
            {
                SuppressNotifications();
                foreach (var item in FilteredChangesets)
                {
                    if (item.IsHighlighted && !item.IsSelected)
                    {
                        item.IsSelected = true;
                        _changesetsToResetOnCancel.Add(new Tuple<TfsChangesetWrapper, bool>(item, false));
                    }
                }
            }
            finally
            {
                AllowNotifications();
            }


            if (!_changesetHighlightUpdatePending)
            {
                _changesetHighlightUpdatePending = true;
                Repository.Instance.BackgroundTaskManager.Post(
                    () =>
                    {
                        HighlightWorkItems();
                        return true;
                    });
            }
        }

        void ShowWorkItemView()
        {
            Repository.Instance.VMergeUIProvider.FocusWorkItemWindow();
        }

        void ViewChangeset(object o)
        {
            try
            {
                TfsChangesetWrapper wrap = (TfsChangesetWrapper)o;
                Repository.Instance.TfsUIInteractionProvider.ShowChangeset(wrap.TfsChangeset.Changeset.ChangesetId);
            }
            catch (Exception)
            {
            }
        }

        void TrackChangeset(object o)
        {
            try
            {
                TfsChangesetWrapper wrap = (TfsChangesetWrapper)o;
                Repository.Instance.TfsUIInteractionProvider.TrackChangeset(wrap.TfsChangeset.Changeset.ChangesetId);
            }
            catch (Exception)
            {
            }
        }

        void ApplyFilter()
        {
            Regex reInc = null, reExc = null, reUser = null;
            if (IncludeCommentFilterActive)
            {
                try
                {
                    reInc = new Regex(IncludeCommentFilter);
                }
                catch (Exception)
                {
                    _applyFilterTimer.Stop();
                    return;
                }
            }
            if (ExcludeCommentFilterActive)
            {
                try
                {
                    reExc = new Regex(ExcludeCommentFilter);
                }
                catch (Exception)
                {
                    _applyFilterTimer.Stop();
                    return;
                }
            }
            if (IncludeByUserFilterActive)
            {
                try
                {
                    reUser = new Regex(IncludeUserFilter.ToLower());
                }
                catch (Exception)
                {
                    _applyFilterTimer.Stop();
                    return;
                }
            }

            foreach (var cs in Changesets)
            {
                if (reInc != null)
                {
                    if (!reInc.IsMatch(cs.TfsChangeset.Description))
                        continue;
                }
                if (reExc != null)
                {
                    if (reExc.IsMatch(cs.TfsChangeset.Description))
                        continue;
                }
                if (reUser != null)
                {
                    if (!reUser.IsMatch(cs.TfsChangeset.Changeset.OwnerDisplayName.ToLower()))
                        continue;
                }
                if (DateFromFilterActive)
                {
                    if (cs.TfsChangeset.Changeset.CreationDate < DateFromFilter)
                        continue;
                }
                if (DateToFilterActive)
                {
                    if (cs.TfsChangeset.Changeset.CreationDate > DateToFilter)
                        continue;
                }
                break;
            }

            _lastApplyFilterTimerTick = DateTime.Now;
            _applyFilterTimer.Stop();
            _applyFilterTimer.Start();
            _applyFilterTimer.IsEnabled = true;
        }

        void PerformApplyFilter()
        {
            _applyFilterTimer.Stop();
            _applyFilterTimer.IsEnabled = false;
            var defaultProfile = Repository.Instance.ProfileProvider.GetDefaultProfile();
            if (defaultProfile == null)
                return;

            try
            {
                DetachFromProfileProvider();
                defaultProfile.ChangesetIncludeCommentFilter = IncludeCommentFilterActive ? IncludeCommentFilter : null;
                defaultProfile.ChangesetExcludeCommentFilter = ExcludeCommentFilterActive ? ExcludeCommentFilter : null;
                defaultProfile.DateFromFilter = DateFromFilterActive ? DateFromFilter : null;
                defaultProfile.DateToFilter = DateToFilterActive ? DateToFilter : null;
                defaultProfile.ChangesetIncludeUserFilter = IncludeByUserFilterActive ? IncludeUserFilter : null;

            }
            finally
            {
                AttachToProfileProvider();
            }

            Regex reInc = null, reExc = null, reUser = null;
            if (IncludeCommentFilterActive)
            {
                try
                {
                    reInc = new Regex(IncludeCommentFilter);
                }
                catch(Exception)
                {
                    reInc = null;
                }
            }
            if (ExcludeCommentFilterActive)
            {
                try
                {
                    reExc = new Regex(ExcludeCommentFilter);
                }
                catch (Exception)
                {
                    reExc = null;
                }
            }
            if (_includeByUserFilterActive)
            {
                try
                {
                    reUser = new Regex(IncludeUserFilter.ToLower());
                }
                catch (Exception)
                {
                    reUser = null;
                }
            }
            var newFilteredChangesets = new ObservableCollection<TfsChangesetWrapper>();
            foreach(var cs in Changesets)
            {
                if (reInc!=null)
                {
                    if (!reInc.IsMatch(cs.TfsChangeset.Description))
                        continue;
                }
                if (reExc!=null)
                {
                    if (reExc.IsMatch(cs.TfsChangeset.Description))
                        continue;
                }
                if (reUser != null)
                {
                    if (!reUser.IsMatch(cs.TfsChangeset.Changeset.OwnerDisplayName.ToLower()))
                        continue;
                }
                if (DateFromFilterActive)
                {
                    if (cs.TfsChangeset.Changeset.CreationDate<DateFromFilter)
                        continue;
                }
                if (DateToFilterActive)
                {
                    if (cs.TfsChangeset.Changeset.CreationDate>DateToFilter)
                        continue;
                }
                newFilteredChangesets.Add(cs);
            }
            
            FilteredChangesets = newFilteredChangesets;
            ChangesetList.SetContent(FilteredChangesets);
            foreach (var item in TfsItemCache.ChangesetCache)
            {
                item.IsVisible = FilteredChangesets.Contains(item);
            }
        }

        void Merge(object o)
        {
            try
            {
                var candidates = FilteredChangesets.Where(cs => cs.IsSelected).Select(cs => cs.TfsChangeset);

                if (!candidates.Any())
                {
                    var mbvm = new MessageBoxViewModel("Perform merge", "No changesets are currently selected.", MessageBoxViewModel.MessageBoxButtons.OK);
                    Repository.Instance.ViewManager.ShowModal(mbvm);
                }
                else
                {
                    var mergeViewModel = new PrepareMergeViewModel(TfsItemCache, candidates);
                    var result = ViewSelectionViewModel.GetMergeSourceTargetBranches();
                    mergeViewModel.MergeSource = result != null ? result.SourceBranch : null;
                    mergeViewModel.MergeTarget = result != null ? result.TargetBranch : null;
                    mergeViewModel.PathFilter = result != null ? result.PathFilter : null;
                    mergeViewModel.SetDefaults();

                    if (Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.PerformNonModalMergeKey))
                    {
                        vMergePackage.OpenMergeView(mergeViewModel);
                    }
                    else
                    {
                        Repository.Instance.ViewManager.ShowModal(mergeViewModel, "Modal");
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, false);
            }
        }

        void ShowMergeView()
        {
            Repository.Instance.VMergeUIProvider.FocusMergeWindow();
        }
        #endregion

        #region Abstract Methods Implementation
        protected override void SaveInternal(object data)
        {
            if (IsDirty)
            {
                IsDirty = false;
                var settings = (data as ISettings);
                settings.SetSettings("ChangesetViewModel.ColumnSettings", ChangesetList.Columns);
            }
        }
        #endregion
    }
}
