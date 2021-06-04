using alexbegh.Utility.Commands;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.UserControls.LoadingProgress;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.Utility.Helpers.Logging;

namespace alexbegh.vMerge.ViewModel.ViewSelection
{
    class ViewSelectionViewModel : BaseViewModel
    {
        #region Public Enum Types
        public enum ViewTypeEnum
        {
            MergeCandidates,
            WorkItemQuery
        }
        #endregion

        #region Inner Classes
        public class ViewTypeSelection
        {
            public ViewTypeEnum ViewType { get; set; }
            public string Description { get; set; }
        }

        public class MergeCandidateSelection
        {
            public ITfsBranch SourceBranch { get; set; }
            public ITfsBranch TargetBranch { get; set; }
            public string PathFilter { get; set; }
        }
        #endregion

        #region Static Constructor
        static ViewSelectionViewModel()
        {
            AddDependency<ViewSelectionViewModel>("SourceBranch", "TargetBranches");
            AddDependency<ViewSelectionViewModel>("TargetBranches", "TargetBranch");
            AddDependency<ViewSelectionViewModel>("ViewType",
                "IsWorkItemQuerySelectionVisible",
                "IsSourceBranchVisible", "IsTargetBranchVisible",
                "IsBranchSelectionVisible");
        }
        #endregion

        #region Constructor
        public ViewSelectionViewModel()
            : base(typeof(ViewSelectionViewModel))
        {
            ViewTypes = new List<ViewTypeSelection>();

            ViewTypes.Add(new ViewTypeSelection()
            {
                ViewType = ViewSelectionViewModel.ViewTypeEnum.MergeCandidates,
                Description = "Display items related to merge candidates"
            });

            ViewTypes.Add(new ViewTypeSelection()
            {
                ViewType = ViewSelectionViewModel.ViewTypeEnum.WorkItemQuery,
                Description = "Select a TFS query"
            });

            AvailableSourceBranchesLoading = new LoadingProgressViewModel();
            RootQueryLoading = new LoadingProgressViewModel();
            ChooseQueryCommand = new RelayCommand((o) => ChooseQuery(o));
            PickPathFilterCommand = new RelayCommand((o) => PickPathFilter());

            AvailableSourceBranchesLoading.IsLoading = !Repository.Instance.TfsBridgeProvider.IsCompleteBranchListLoaded();
            RootQueryLoading.IsLoading = !Repository.Instance.TfsBridgeProvider.IsRootQueryLoaded();

            Repository.Instance.TfsBridgeProvider.ActiveProjectSelected += OnActiveProjectSelected;
            Repository.Instance.TfsBridgeProvider.CompleteBranchListLoaded += OnCompleteBranchListLoaded;
            Repository.Instance.TfsBridgeProvider.RootQueryLoaded += OnRootQueryLoaded;
            Repository.Instance.TfsBridgeProvider.CompleteBranchListLoading += OnCompleteBranchListLoading;
            Repository.Instance.TfsBridgeProvider.RootQueryLoading += OnRootQueryLoading;

            Repository.Instance.BackgroundTaskManager.DelayedPost(() =>
                {
                    return CheckIsLoading();
                });
        }
        #endregion

        #region Properties
        public bool Enabled
        {
            get { return Repository.Instance.TfsBridgeProvider.ActiveTeamProject != null; }
        }

        private bool _showViewOptions;
        public bool ShowViewOptions
        {
            get { return _showViewOptions; }
            set { Set(ref _showViewOptions, value); }
        }

        private LoadingProgressViewModel _availableSourceBranchesLoading;
        public LoadingProgressViewModel AvailableSourceBranchesLoading
        {
            get { return _availableSourceBranchesLoading; }
            set { Set(ref _availableSourceBranchesLoading, value); }
        }

        private LoadingProgressViewModel _rootQueryLoading;
        public LoadingProgressViewModel RootQueryLoading
        {
            get { return _rootQueryLoading; }
            set { Set(ref _rootQueryLoading, value); }
        }

        private ViewTypeEnum _viewType;
        public ViewTypeEnum ViewType
        {
            get { return _viewType; }
            set { Set(ref _viewType, value); }
        }

        private List<ViewTypeSelection> _viewTypes;
        public List<ViewTypeSelection> ViewTypes
        {
            get { return _viewTypes; }
            set { Set(ref _viewTypes, value); }
        }

        public bool IsWorkItemQuerySelectionVisible
        {
            get { return _viewType == ViewTypeEnum.WorkItemQuery; }
        }

        public bool IsSourceBranchVisible
        {
            get { return _viewType == ViewTypeEnum.MergeCandidates; }
        }

        public bool IsTargetBranchVisible
        {
            get { return _viewType == ViewTypeEnum.MergeCandidates; }
        }

        public bool IsBranchSelectionVisible
        {
            get { return IsSourceBranchVisible || IsTargetBranchVisible; }
        }

        private List<ITfsBranch> _availableSourceBranches;
        public IReadOnlyList<ITfsBranch> AvailableSourceBranches
        {
            get
            {
                Repository.ThrowIfNotUIThread();
                if (SourceBranch != null && SourceBranch.IsSubBranch)
                    return _availableSourceBranches.AsEnumerable().Concat(new[] { SourceBranch }).ToList().AsReadOnly();
                else
                    return _availableSourceBranches;
            }
            private set
            {
                Repository.ThrowIfNotUIThread();
                var oldSourceBranch = _sourceBranch;
                Set(ref _availableSourceBranches, value as List<ITfsBranch>,
                    () => 
                        {
                            if (oldSourceBranch == null || value == null)
                                _sourceBranch = null;
                            else
                                _sourceBranch = value.FirstOrDefault(branch => branch.Name == oldSourceBranch.Name);
                        });
            }
        }

        private List<ITfsBranch> _targetBranches;
        public IReadOnlyList<ITfsBranch> TargetBranches
        {
            get
            {
                Repository.ThrowIfNotUIThread();

                if (!Model.Repository.Instance.TfsBridgeProvider.IsCompleteBranchListLoaded() || SourceBranch == null)
                    return null;

                if (_targetBranches != null)
                    return _targetBranches;

                TargetBranch = null;
                _targetBranches = new List<ITfsBranch>(
                    Repository.Instance.TfsBridgeProvider.GetPossibleMergeTargetBranches(SourceBranch));
                if (_targetBranches.Count == 1)
                    TargetBranch = _targetBranches.First();
                return _targetBranches;
            }
            private set
            {
                Repository.ThrowIfNotUIThread();
                Set(ref _targetBranches, value as List<ITfsBranch>,
                    () => _targetBranch = null);
            }
        }

        private ITfsBranch _sourceBranch;
        public ITfsBranch SourceBranch
        {
            get
            {
                Repository.ThrowIfNotUIThread();
                return _sourceBranch;
            }
            set
            {
                Repository.ThrowIfNotUIThread();
                var oldBranch = _sourceBranch;
                Set(ref _sourceBranch, value,
                    () => _targetBranches = null);
                if (_sourceBranch==null)
                {
                    _pathFilter = null;
                    RaisePropertyChanged("PathFilter");
                }
                if (_sourceBranch != null)
                {
                    if (_pathFilter!=null && oldBranch!=null)
                    {
                        var newTarget = Repository.Instance.TfsBridgeProvider.GetPathInTargetBranch(oldBranch, _pathFilter);
                        if (newTarget != null)
                            _pathFilter = newTarget;
                    }
                    if (_pathFilter == null || !PathFilter.StartsWith(_sourceBranch.Name, StringComparison.InvariantCultureIgnoreCase))
                        _pathFilter = SourceBranch.Name;
                    if (_pathFilter != null)
                    {
                        _pathFilter = _sourceBranch.Name;
                        RaisePropertyChanged("PathFilter");
                    }
                    if (_sourceBranch.IsSubBranch || (oldBranch != null && _sourceBranch.IsSubBranch != oldBranch.IsSubBranch))
                        RaisePropertyChanged("AvailableSourceBranches");
                }
            }
        }

        private ITfsBranch _targetBranch;
        public ITfsBranch TargetBranch
        {
            get
            {
                Repository.ThrowIfNotUIThread();
                return _targetBranch;
            }
            set
            {
                Repository.ThrowIfNotUIThread();
                Set(ref _targetBranch, value);
            }
        }

        private ITfsQueryFolder _rootQuery;
        public ITfsQueryFolder RootQuery
        {
            get
            {
                Repository.ThrowIfNotUIThread();
                return _rootQuery;
            }
            set
            {
                Repository.ThrowIfNotUIThread();
                var old = _rootQuery;
                Set(ref _rootQuery, value, () =>
                    {
                        try
                        {
                            if (_rootQuery!=null && SelectedQuery != null && _rootQuery.All.Contains(SelectedQuery) == false)
                            {
                                foreach (var item in _rootQuery.All)
                                {
                                    if (item.QualifiedTitle == SelectedQuery.QualifiedTitle)
                                    {
                                        SelectedQuery = item as ITfsQuery;
                                        return;
                                    }
                                }
                            }
                            SelectedQuery = null;
                        }
                        catch(Exception ex)
                        {
                            SimpleLogger.Log(ex, true);
                        }
                    });
            }
        }

        private ITfsQuery _selectedQuery;
        public ITfsQuery SelectedQuery
        {
            get { return _selectedQuery; }
            set { Set(ref _selectedQuery, value); }
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
                if (!Repository.Instance.TfsBridgeProvider.IsCompleteBranchListLoaded())
                    return;
                Set(ref _pathFilter, value, () =>
                {
                    if (_pathFilter != null)
                    {
                        var matchingBranch
                            = Repository.Instance.TfsBridgeProvider.CompleteBranchList
                                .Where(branch => _pathFilter.StartsWith(branch.Name, StringComparison.InvariantCultureIgnoreCase))
                                .OrderByDescending(branch => branch.Name.Length)
                                .FirstOrDefault();
                        SourceBranch = matchingBranch;
                    }
                });
            }
        }

        private RelayCommand _chooseQueryCommand;
        public RelayCommand ChooseQueryCommand
        {
            get { return _chooseQueryCommand; }
            set { Set(ref _chooseQueryCommand, value); }
        }

        private RelayCommand _pickPathFilterCommand;
        public RelayCommand PickPathFilterCommand
        {
            get { return _pickPathFilterCommand; }
            set { Set(ref _pickPathFilterCommand, value); }
        }

        private event EventHandler _viewSelectionChanged;
        public event EventHandler ViewSelectionChanged
        {
            add { _viewSelectionChanged += value; }
            remove { _viewSelectionChanged -= value; }
        }
        #endregion

        #region Private Methods
        private void CheckSelectionFinishedEvent()
        {
            if (Model.Repository.Instance.TfsBridgeProvider.ActiveTeamProject == null
                || !Model.Repository.Instance.TfsBridgeProvider.IsRootQueryLoaded()
                || !Model.Repository.Instance.TfsBridgeProvider.IsCompleteBranchListLoaded())
                return;

            bool raise = false;
            if (IsQuerySelected())
                raise = true;
            else if (IsMergeCandidatesSelected())
                raise = true;

            if (raise)
            {
                if (_viewSelectionChanged != null)
                    _viewSelectionChanged(this, new EventArgs());
            }
        }
        #endregion

        #region Event Handlers
        private void OnCompleteBranchListLoaded(object sender, EventArgs e)
        {
            if (Model.Repository.Instance.TfsBridgeProvider.CompleteBranchList == null)
            {
                AvailableSourceBranchesLoading.IsLoading = false;
                AvailableSourceBranches = null;
                TargetBranches = null;
                return;
            }
            try
            {
                Repository.ThrowIfNotUIThread();

                AvailableSourceBranches =
                    Model.Repository.Instance.TfsBridgeProvider.CompleteBranchList.Where(branch => branch.Name.StartsWith(Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ServerItem, StringComparison.InvariantCultureIgnoreCase)).ToList();
                TargetBranches = null;
                AvailableSourceBranchesLoading.IsLoading = false;
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true);
            }
        }

        private void OnActiveProjectSelected(object sender, EventArgs e)
        {
            try
            {
                Repository.ThrowIfNotUIThread();
                AvailableSourceBranches = null;
                TargetBranches = null;
                SourceBranch = null;
                TargetBranch = null;
                SelectedQuery = null;
                RootQuery = null;
                RaisePropertyChanged("Enabled");
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true);
            }
        }

        private void OnRootQueryLoaded(object sender, EventArgs e)
        {
            try
            {
                Repository.ThrowIfNotUIThread();
                RootQuery = Repository.Instance.TfsBridgeProvider.RootQuery;
                RootQueryLoading.IsLoading = false;
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true);
            }
        }

        private void OnCompleteBranchListLoading(object sender, EventArgs e)
        {
            AvailableSourceBranchesLoading.IsLoading = true;
            Repository.Instance.BackgroundTaskManager.DelayedPost(() =>
            {
                return CheckIsLoading();
            });
        }

        private void OnRootQueryLoading(object sender, EventArgs e)
        {
            RootQueryLoading.IsLoading = true;
            Repository.Instance.BackgroundTaskManager.DelayedPost(() =>
            {
                return CheckIsLoading();
            });
        }

        private bool CheckIsLoading()
        {
            if (Repository.Instance.TfsBridgeProvider.IsCompleteBranchListLoaded())
                AvailableSourceBranchesLoading.IsLoading = false;
            if (Repository.Instance.TfsBridgeProvider.IsRootQueryLoaded())
                RootQueryLoading.IsLoading = false;
            return !(AvailableSourceBranchesLoading.IsLoading || RootQueryLoading.IsLoading);
        }
        #endregion

        #region Command Handlers
        void ChooseQuery(object q)
        {
            SelectedQuery = (q as ITfsQuery);
        }

        void PickPathFilter()
        {
            var result = Repository.Instance.TfsUIInteractionProvider.BrowseForTfsFolder(PathFilter);
            if (result != null)
            {
                PathFilter = result;
            }
        }
        #endregion

        #region Abstract Methods Implementation
        protected override void SaveInternal(object data)
        {
        }
        #endregion

        #region Protected Overridden Methods
        protected override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);
            if (propertyName == "ShowViewOptions")
                return;

            CheckSelectionFinishedEvent();
        }
        #endregion

        #region Public Methods
        public bool IsQuerySelected()
        {
            return ViewType == ViewTypeEnum.WorkItemQuery && SelectedQuery != null;
        }

        public ITfsQuery GetSelectedQuery()
        {
            if (!IsQuerySelected())
                return null;
            return SelectedQuery;
        }

        public bool IsMergeCandidatesSelected()
        {
            return ViewType == ViewTypeEnum.MergeCandidates && SourceBranch != null && TargetBranch != null;
        }

        public MergeCandidateSelection GetMergeSourceTargetBranches()
        {
            if (!IsMergeCandidatesSelected())
                return null;

            return new MergeCandidateSelection()
            {
                SourceBranch = this.SourceBranch,
                TargetBranch = this.TargetBranch,
                PathFilter = this.PathFilter
            };
        }

        public void PrepareRefresh()
        {
            _availableSourceBranches = null;
            _sourceBranch = null;
            _targetBranches = null;
            _targetBranch = null;
            _selectedQuery = null;
        }
        #endregion
    }
}
