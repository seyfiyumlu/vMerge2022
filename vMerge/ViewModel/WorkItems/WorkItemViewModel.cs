using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using alexbegh.Utility.Commands;
using alexbegh.Utility.Managers.Background;
using alexbegh.Utility.UserControls.FieldMapperGrid;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using alexbegh.vMerge.StudioIntegration.Framework;
using alexbegh.vMerge.ViewModel.ViewSelection;
using alexbegh.vMerge.ViewModel.Wrappers;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Diagnostics;
using alexbegh.vMerge.ViewModel.Merge;
using alexbegh.Utility.Helpers.ViewModel;

namespace alexbegh.vMerge.ViewModel.WorkItems
{
    class WorkItemViewModel : TfsListBaseViewModel
    {
        #region Private Methods
        private void AttachToWorkItemCache()
        {
            ((INotifyCollectionChanged)TfsItemCache.WorkItemCache).CollectionChanged +=
                (c, a) =>
                {
                    if (a.NewItems != null)
                    {
                        foreach (var item in a.NewItems.Cast<TfsWorkItemWrapper>())
                        {
                            item.PropertyChanged += WorkItemPropertyChanged;
                        }
                    }

                    if (a.OldItems != null)
                    {
                        foreach (var item in a.OldItems.Cast<TfsWorkItemWrapper>())
                        {
                            item.PropertyChanged -= WorkItemPropertyChanged;
                        }
                    }
                };

            foreach (var item in TfsItemCache.WorkItemCache)
            {
                item.PropertyChanged += WorkItemPropertyChanged;
            }

            TfsItemCache.WorkItemHasBeenMerged += TfsItemCache_WorkItemHasBeenMerged;
        }

        private void TfsItemCache_WorkItemHasBeenMerged(object sender, WorkItemHasBeenMergedEventArgs e)
        {
            if (ViewSelectionViewModel.IsMergeCandidatesSelected())
            {
                var original = WorkItemList.OriginalElements.Where(changeset => changeset == e.WorkItem).FirstOrDefault();
                if (original != null)
                    WorkItemList.OriginalElements.Remove(original);

                var source = WorkItemList.Elements.Where(changeset => changeset.InnerItem == e.WorkItem).FirstOrDefault();
                if (source != null)
                    WorkItemList.Elements.Remove(source);
            }
        }

        protected override IEnumerable<FieldMapperGridColumn> GetColumns()
        {
            var data = Repository.Instance.Settings.FetchSettings<ObservableCollection<FieldMapperGridColumn>>("WorkItemViewModel.ColumnSettings");
            if (data != null)
            {
                foreach (var column in data)
                    yield return column;
            }
            else
            {
                yield return new FieldMapperGridColumn() { Column = "IsSelected", Header = "IsSelected", Visible = true, Width = 100, Type=typeof(bool) };
                yield return new FieldMapperGridColumn() { Column = "ID", Header = "ID", Visible = true, Width = 70, Type=typeof(int) };
                yield return new FieldMapperGridColumn() { Column = "Work Item Type", Header = "Work Item Type", Visible = true, Width = 100, Type=typeof(string) };
                yield return new FieldMapperGridColumn() { Column = "State", Header = "State", Visible = true, Width = 100, Type=typeof(string) };
                yield return new FieldMapperGridColumn() { Column = "Title", Header = "Title", Visible = true, Width = 200, Type=typeof(string) };
                yield return new FieldMapperGridColumn() { Column = "Description", Header = "Description", Visible = true, Width = 200, Type=typeof(MultiLineString) };
            }
        }

        protected override void UpdateColumns(ObservableCollection<FieldMapperGridColumn> columns)
        {
            WorkItemList.SetColumns(columns);
            Columns = columns;
        }

        private void ResetWorkItems()
        {
            WorkItems.Clear();
        }

        private void LoadWorkItems()
        {
            if (Repository.Instance.TfsConnectionInfo.Uri == null)
            {
                ResetWorkItems();
                return;
            }

            if (ViewSelectionViewModel.IsMergeCandidatesSelected())
            {
                var result = ViewSelectionViewModel.GetMergeSourceTargetBranches();
                ResetWorkItems();

                Repository.Instance.BackgroundTaskManager.Start(
                    Constants.Tasks.LoadWorkItemsTaskKey,
                    ItemsLoading,
                    (task) =>
                    {
                        LoadWorkItems(result.SourceBranch, result.TargetBranch, result.PathFilter, task);
                    });
            }
            else if (ViewSelectionViewModel.IsQuerySelected())
            {
                var query = ViewSelectionViewModel.GetSelectedQuery();
                ResetWorkItems();

                Repository.Instance.BackgroundTaskManager.Start(
                    Constants.Tasks.LoadWorkItemsTaskKey,
                    ItemsLoading,
                    (task) =>
                    {
                        LoadWorkItems(query, task);
                    });
            }
            else
            {
                ItemsLoading.IsLoading = false;
            }
        }

        private void SetContent(ObservableCollection<TfsWorkItemWrapper> workItems)
        {
            if (Repository.Instance.TfsConnectionInfo.Uri == null)
            {
                ResetWorkItems();
                return;
            }

            WorkItems = workItems;
            WorkItemList.SetContent(WorkItems);
            foreach (var item in TfsItemCache.WorkItemCache)
            {
                item.IsVisible = WorkItems.Contains(item);
            }
        }

        private void LoadWorkItems(ITfsBranch sourceBranch, ITfsBranch targetBranch, string pathFilter, BackgroundTask task)
        {
            task.TrackProgress.ProgressInfo = "Loading merge candidates ...";
            var changesets = new List<TfsChangesetWrapper>(TfsItemCache.QueryChangesets(sourceBranch, targetBranch, pathFilter));
            task.TrackProgress.ProgressInfo = "Loading work items ...";
            task.TrackProgress.MaxProgress = changesets.Count;
            var workItems
                = new ObservableCollection<TfsWorkItemWrapper>(
                    TfsItemCache.QueryWorkItems(changesets, task.TrackProgress, task.Cancelled.Token));

            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    if (!task.Cancelled.IsCancellationRequested)
                        SetContent(workItems);
                    return true;
                });
        }

        private void LoadWorkItems(ITfsQuery query, BackgroundTask task)
        {
            task.TrackProgress.ProgressInfo = "Loading work items ...";
            var workItems = new ObservableCollection<TfsWorkItemWrapper>(
                TfsItemCache.QueryWorkItems(query));

            Repository.Instance.BackgroundTaskManager.Post(
                () =>
                {
                    try
                    {
                        if (!task.Cancelled.IsCancellationRequested)
                            SetContent(workItems);
                    }
                    catch(Exception)
                    {
                        return true;
                    }
                    return true;
                });
        }

        private void HighlightChangesets(TfsWorkItemWrapper sender)
        {
            _workItemHighlightUpdatePending = false;

            if (!Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                (trackProgressParameters) => TfsItemCache.HighlightChangesets(trackProgressParameters),
                "Please wait while the changeset view is being updated ..."))
            {
                sender.IsSelected = false;
            }
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
        static WorkItemViewModel()
        {
            AddDependency<WorkItemViewModel>("Enabled", "ShowPendingMergeInfo");
        }
        #endregion

        #region Constructor
        public WorkItemViewModel(TfsItemCache tfsItemCache)
            : base("Work Item", typeof(WorkItemViewModel))
        {
            Repository.Instance.VMergeUIProvider.MergeWindowVisibilityChanged += (o, a) => Enabled = !Repository.Instance.VMergeUIProvider.IsMergeWindowVisible();
            Enabled = !Repository.Instance.VMergeUIProvider.IsMergeWindowVisible();
            TfsItemCache = tfsItemCache;

            ViewSelectionViewModel = new ViewSelectionViewModel();
            WorkItems = new ObservableCollection<TfsWorkItemWrapper>();
            WorkItemList =
                new FieldMapperGridViewModel<TfsWorkItemWrapper>(
                    new WorkItemPropertyAccessor(),
                    WorkItems,
                    Columns);
            WorkItemList.ColumnSettingsChanged += (o, a) => DataWasModified();

            ItemSelectedCommand = new RelayCommand((o) => ItemSelected(o));
            ShowChangesetViewCommand = new RelayCommand((o) => ShowChangesetView());
            SelectMarkedItemsCommand = new RelayCommand((o) => SelectMarkedItems());
            ViewWorkItemCommand = new RelayCommand((o) => ViewWorkItem(o));
            TrackWorkItemCommand = new RelayCommand((o) => TrackWorkItem(o));
            MergeCommand = new RelayCommand((o) => Merge(o));
            ShowMergeViewCommand = new RelayCommand((o) => ShowMergeView());
            RefreshCommand = new RelayCommand((o) => Refresh(), (o) => Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection != null);

            ViewSelectionViewModel.ViewSelectionChanged += OnViewSelectionChanged;

            AttachToWorkItemCache();
            AttachToProfileProvider();
            Repository.Instance.TfsBridgeProvider.ActiveProjectSelected += (o, a) =>
            {
                WorkItems.Clear();
                if (Repository.Instance.TfsBridgeProvider.ActiveTeamProject != null)
                    Repository.Instance.BackgroundTaskManager.DelayedPost(() => { ProfileProvider_DefaultProfileChanged(null, null); return true; });
            };
        }
        #endregion

        #region Private Event Handlers
        private void WorkItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SuppressNotificationsCounter > 0)
                return;

            if (e.PropertyName == "IsSelected")
            {
                try
                {
                    SuppressNotifications();
                    var senderWorkItem = (sender as TfsWorkItemWrapper);
                    foreach (var other in WorkItemList.SelectedElements)
                    {
                        other.IsSelected = senderWorkItem.IsSelected;
                    }
                }
                finally
                {
                    AllowNotifications();
                }

                if (!_workItemHighlightUpdatePending)
                {
                    _workItemHighlightUpdatePending = true;
                    Repository.Instance.BackgroundTaskManager.Post(
                        () =>
                        {
                            HighlightChangesets(sender as TfsWorkItemWrapper);
                            return true;
                        });
                }
            }
        }

        private void OnViewSelectionChanged(object sender, EventArgs e)
        {
            LoadWorkItems();
            ViewSelectionViewModel.ShowViewOptions = false;
            var defaultProfile = Repository.Instance.ProfileProvider.GetDefaultProfile();

            try
            {
                DetachFromProfileProvider();
                if (ViewSelectionViewModel.IsMergeCandidatesSelected())
                {
                    var result = ViewSelectionViewModel.GetMergeSourceTargetBranches();
                    defaultProfile.WIQueryName = null;
                    defaultProfile.WISourceBranch = result.SourceBranch.Name;
                    defaultProfile.WITargetBranch = result.TargetBranch.Name;
                    defaultProfile.WISourcePathFilter = result.PathFilter;
                }
                else if (ViewSelectionViewModel.IsQuerySelected())
                {
                    var query = ViewSelectionViewModel.GetSelectedQuery();
                    defaultProfile.WISourceBranch = null;
                    defaultProfile.WITargetBranch = null;
                    defaultProfile.WISourcePathFilter = null;
                    defaultProfile.WIQueryName = query.QualifiedTitle;
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
                
            if (defaultProfile.WIQueryName != null)
            {
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
                        = ViewSelectionViewModel.RootQuery.All.Where(query => query.QualifiedTitle == defaultProfile.WIQueryName).FirstOrDefault() as ITfsQuery;
                }
                return;
            }
            if (defaultProfile.WISourceBranch != null)
            {
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
                    ViewSelectionViewModel.SourceBranch =
                        ViewSelectionViewModel.AvailableSourceBranches.Where(branch => branch.Name == defaultProfile.WISourceBranch).FirstOrDefault();
                    if (ViewSelectionViewModel.TargetBranches != null)
                    {
                        ViewSelectionViewModel.TargetBranch =
                            ViewSelectionViewModel.TargetBranches.Where(branch => branch.Name == defaultProfile.WITargetBranch).FirstOrDefault();
                    }
                    ViewSelectionViewModel.PathFilter = defaultProfile.WISourcePathFilter;
                }
                return;
            }
        }
        #endregion

        #region Private Fields/Properties
        private bool _workItemHighlightUpdatePending;

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

        private ObservableCollection<TfsWorkItemWrapper> _workItems;
        public ObservableCollection<TfsWorkItemWrapper> WorkItems
        {
            get { return _workItems; }
            set { Set(ref _workItems, value); }
        }

        private FieldMapperGridViewModel<TfsWorkItemWrapper> _workItemList;
        public FieldMapperGridViewModel<TfsWorkItemWrapper> WorkItemList
        {
            get
            {
                return _workItemList;
            }
            set
            {
                Set(ref _workItemList, value);
            }
        }
        #endregion

        #region Command Properties
        private RelayCommand _itemSelectedCommand;
        public RelayCommand ItemSelectedCommand
        {
            get { return _itemSelectedCommand; }
            set { Set(ref _itemSelectedCommand, value); }
        }

        private RelayCommand _showChangesetViewCommand;
        public RelayCommand ShowChangesetViewCommand
        {
            get { return _showChangesetViewCommand; }
            set { Set(ref _showChangesetViewCommand, value); }
        }

        private RelayCommand _selectMarkedItemsCommand;
        public RelayCommand SelectMarkedItemsCommand
        {
            get { return _selectMarkedItemsCommand; }
            set { Set(ref _selectMarkedItemsCommand, value); }
        }

        private RelayCommand _viewWorkItemCommand;
        public RelayCommand ViewWorkItemCommand
        {
            get { return _viewWorkItemCommand; }
            set { Set(ref _viewWorkItemCommand, value); }
        }

        private RelayCommand _trackWorkItemCommand;
        public RelayCommand TrackWorkItemCommand
        {
            get { return _trackWorkItemCommand; }
            set { Set(ref _trackWorkItemCommand, value); }
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
        #endregion

        #region Command Handlers
        void ItemSelected(object o)
        {
        }

        void SelectMarkedItems()
        {
            TfsWorkItemWrapper firstItem = null;
            try
            {
                SuppressNotifications();
                foreach (var item in WorkItems)
                {
                    if (item.IsHighlighted)
                    {
                        if (firstItem==null)
                            firstItem = item;
                        item.IsSelected = true;
                    }
                }
            }
            finally
            {
                AllowNotifications();
            }

            if (!_workItemHighlightUpdatePending)
            {
                _workItemHighlightUpdatePending = true;
                Repository.Instance.BackgroundTaskManager.Post(
                    () =>
                    {
                        HighlightChangesets(firstItem);
                        return true;
                    });
            }
        }

        void ShowChangesetView()
        {
            Repository.Instance.VMergeUIProvider.FocusChangesetWindow();
        }

        void ViewWorkItem(object o)
        {
            try
            {
                TfsWorkItemWrapper wrap = (TfsWorkItemWrapper)o;
                Repository.Instance.TfsUIInteractionProvider.ShowWorkItem(wrap.TfsWorkItem.Id);
            }
            catch (Exception)
            {
            }
        }

        void TrackWorkItem(object o)
        {
            try
            {
                TfsWorkItemWrapper wrap = (TfsWorkItemWrapper)o;
                Repository.Instance.TfsUIInteractionProvider.TrackWorkItem(wrap.TfsWorkItem.Id);
            }
            catch (Exception)
            {
            }
        }

        void Merge(object o)
        {
            try
            {
                var candidates = WorkItems.Where(wi => wi.IsSelected).Select(wi => wi.TfsWorkItem);

                if (!candidates.Any())
                {
                    var mbvm = new MessageBoxViewModel("Perform merge", "No work items are currently selected.", MessageBoxViewModel.MessageBoxButtons.OK);
                    Repository.Instance.ViewManager.ShowModal(mbvm);
                }
                else if (!candidates.Any(candidate => candidate.RelatedChangesetCount>0))
                {
                    var mbvm = new MessageBoxViewModel("Perform merge", "The selected work items don't have any linked changesets.", MessageBoxViewModel.MessageBoxButtons.OK);
                    Repository.Instance.ViewManager.ShowModal(mbvm);
                }
                else
                {
                    PrepareMergeViewModel mergeViewModel;
                    try
                    {
                        mergeViewModel = new PrepareMergeViewModel(TfsItemCache, WorkItems.Where(wi => wi.IsSelected).Select(wi => wi.TfsWorkItem));
                    }
                    catch(ArgumentException)
                    {
                        var mbvm = new MessageBoxViewModel(
                            "Perform merge", 
                            String.Format("The selected work items don't have any linked changesets in the current team project\r\n({0}).", Repository.Instance.TfsBridgeProvider.ActiveTeamProject.Name),
                            MessageBoxViewModel.MessageBoxButtons.OK);
                        Repository.Instance.ViewManager.ShowModal(mbvm);
                        mergeViewModel = null;
                    }

                    if (mergeViewModel!=null)
                    {
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
            }
            catch (Exception)
            {
            }
        }

        void ShowMergeView()
        {
            Repository.Instance.VMergeUIProvider.FocusMergeWindow();
        }

        protected override void Refresh()
        {
            base.Refresh();
            ItemsLoading.IsLoading = true;
            ItemsLoading.ProgressInfo = "Reloading projects, branches and changesets ... please wait";
            ResetWorkItems();
            SetContent(new ObservableCollection<TfsWorkItemWrapper>());
            TfsItemCache.Clear();
            Repository.Instance.TfsBridgeProvider.Refresh();
        }

        #endregion

        #region Abstract Methods Implementation
        protected override void SaveInternal(object data)
        {
            if (IsDirty)
            {
                IsDirty = false;
                var settings = (data as ISettings);
                settings.SetSettings("WorkItemViewModel.ColumnSettings", WorkItemList.Columns);
            }
        }
        #endregion
    }
}
