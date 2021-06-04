using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using alexbegh.Utility.Commands;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.UserControls.FieldMapperGrid;
using alexbegh.Utility.UserControls.LoadingProgress;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using alexbegh.vMerge.ViewModel.Configuration;

namespace alexbegh.vMerge.ViewModel
{
    abstract class TfsListBaseViewModel : BaseViewModel
    {
        #region Public Properties
        private string _configurationName;
        public string ConfigurationName
        {
            get { return _configurationName; }
            private set { Set(ref _configurationName, value); }
        }

        private bool _showViewOptions;
        public bool ShowViewOptions
        {
            get { return _showViewOptions; }
            set { Set(ref _showViewOptions, value); }
        }

        private LoadingProgressViewModel _itemsLoading;
        public LoadingProgressViewModel ItemsLoading
        {
            get { return _itemsLoading; }
            set { Set(ref _itemsLoading, value); }
        }

        private ObservableCollection<FieldMapperGridColumn> _columns;
        public ObservableCollection<FieldMapperGridColumn> Columns
        {
            get { return _columns; }
            set { Set(ref _columns, value); }
        }

        private RelayCommand _configureColumnsCommand;
        public RelayCommand ConfigureColumnsCommand
        {
            get { return _configureColumnsCommand; }
            set { Set(ref _configureColumnsCommand, value); }
        }

        private RelayCommand _refreshCommand;
        public RelayCommand RefreshCommand
        {
            get { return _refreshCommand; }
            set { Set(ref _refreshCommand, value); }
        }

        private RelayCommand _saveProfileCommand;
        public RelayCommand SaveProfileCommand
        {
            get { return _saveProfileCommand; }
            set { Set(ref _saveProfileCommand, value); }
        }

        private RelayCommand _loadProfileCommand;
        public RelayCommand LoadProfileCommand
        {
            get { return _loadProfileCommand; }
            set { Set(ref _loadProfileCommand, value); }
        }

        public event EventHandler<EventArgs> ItemsLoaded;

        private ObservableCollection<IProfileSettings> _mergeProfiles;
        public ObservableCollection<IProfileSettings> MergeProfiles
        {
            get { return _mergeProfiles; }
            set { Set(ref _mergeProfiles, value); }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Changesets ViewModel
        /// </summary>
        public TfsListBaseViewModel(string configurationName, Type derived)
            : base(derived)
        {
            ConfigurationName = configurationName;
            ItemsLoading = new LoadingProgressViewModel();
            Columns = new ObservableCollection<FieldMapperGridColumn>(GetColumns());
            ConfigureColumnsCommand = new RelayCommand(
                (o) => ConfigureColumns());
            RefreshCommand = new RelayCommand(
                (o) => Refresh());
            SaveProfileCommand = new RelayCommand(
                (o) => SaveProfile());
            LoadProfileCommand = new RelayCommand(
                (o) => LoadProfile(o as IProfileSettings));

            Repository.Instance.ProfileProvider.ActiveProjectProfileListChanged +=
                (o, a) => ReloadMergeProfiles();
            Repository.Instance.TfsBridgeProvider.ActiveProjectSelected +=
                (o, a) => ReloadMergeProfiles();

            ReloadMergeProfiles();
        }
        #endregion

        #region Private Event Handlers
        private void ConfigureColumns()
        {
            var configurationViewModel = new ConfigureColumnsAndSortOrderViewModel(ConfigurationName, Columns);

            Repository.Instance.ViewManager.ShowModal(configurationViewModel);
        }

        protected virtual void Refresh()
        {
            Repository.Instance.TfsBridgeProvider.Clear();
        }

        private void SaveProfile()
        {
        }

        private void LoadProfile(IProfileSettings profile)
        {
            Repository.Instance.ProfileProvider.LoadProfile(null, profile.Name);
        }

        private void ReloadMergeProfiles()
        {
            MergeProfiles = new ObservableCollection<IProfileSettings>(
                Repository.Instance.ProfileProvider.GetAllProfilesForProject());
        }
        #endregion

        #region Protected abstract Methods
        protected abstract IEnumerable<FieldMapperGridColumn> GetColumns();

        protected abstract void UpdateColumns(ObservableCollection<FieldMapperGridColumn> columns);
        #endregion

        #region Protected Methods
        protected void RaiseItemsLoaded()
        {
            if (ItemsLoaded != null)
                ItemsLoaded(this, null);
        }
        #endregion

        #region Public Methods
        #endregion
    }
}
