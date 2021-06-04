using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alexbegh.Utility.Commands;
using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.Model;
using System.Reflection;
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.ViewModel.Options
{
    class GeneralOptionsViewModel : BaseViewModel, IViewModelIsFinishable, ISettingsChangeListener
    {
        public string VersionNo
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
            }
        }

        public List<string> AvailableThemes
        {
            get;
            private set;
        }

        private bool _hideSplashScreen;
        public bool HideSplashScreen
        {
            get { return _hideSplashScreen; }
            set { Set(ref _hideSplashScreen, value); }
        }

        private string _selectedTheme;
        public string SelectedTheme
        {
            get { return _selectedTheme; }
            set { Set(ref _selectedTheme, value, () => alexbegh.vMerge.StudioIntegration.Framework.vMergePackage.SetTheme(_selectedTheme)); }
        }

        private bool _autoMergeDirectly;
        public bool AutoMergeDirectly
        {
            get
            {
                return _autoMergeDirectly;
            }
            set
            {
                Set(ref _autoMergeDirectly, value);
            }
        }

        private bool _linkMergeWithWorkItems;
        public bool LinkMergeWithWorkItems
        {
            get
            {
                return _linkMergeWithWorkItems;
            }
            set
            {
                Set(ref _linkMergeWithWorkItems, value);
            }
        }

        private bool _performNonModalMerge;
        public bool PerformNonModalMerge
        {
            get
            {
                return _performNonModalMerge;
            }
            set
            {
                Set(ref _performNonModalMerge, value);
            }
        }

        private bool _isPerformNonModalMergeChangeable;
        public bool IsPerformNonModalMergeChangeable
        {
            get
            {
                return _isPerformNonModalMergeChangeable;
            }
            set
            {
                Set(ref _isPerformNonModalMergeChangeable, value);
            }
        }

        private string _tempWorkspaceBasePath;
        public string TempWorkspaceBasePath
        {
            get
            {
                return _tempWorkspaceBasePath;
            }
            set
            {
                Set(ref _tempWorkspaceBasePath, value);
            }
        }

        private RelayCommand _pickTempWorkspaceBasePath;
        public RelayCommand PickTempWorkspaceBasePath
        {
            get
            {
                return _pickTempWorkspaceBasePath;
            }
            set
            {
                Set(ref _pickTempWorkspaceBasePath, value);
            }
        }

        private string _checkInCommentTemplate;
        public string CheckInCommentTemplate
        {
            get
            {
                return _checkInCommentTemplate;
            }
            set
            {
                Set(ref _checkInCommentTemplate, value);
            }
        }

        private RelayCommand _openLogFileFolderCommand;
        public RelayCommand OpenLogFileFolderCommand
        {
            get
            {
                return _openLogFileFolderCommand;
            }
            set
            {
                Set(ref _openLogFileFolderCommand, value);
            }
        }

        private RelayCommand _submitLogFileCommand;
        public RelayCommand SubmitLogFileCommand
        {
            get
            {
                return _submitLogFileCommand;
            }
            set
            {
                Set(ref _submitLogFileCommand, value);
            }
        }

        private bool _storing = false;

        public GeneralOptionsViewModel()
            : base(typeof(GeneralOptionsViewModel))
        {
            Repository.Instance.Settings.AddChangeListener(null, this);
            LoadFromRepository();
            PickTempWorkspaceBasePath = new RelayCommand((o) => PickTempWorkspacePath());
            OpenLogFileFolderCommand = new RelayCommand((o) => OpenLogFileFolder());
            AvailableThemes = new List<string>(MahApps.Metro.ThemeManager.Accents.Select(accent => accent.Name).OrderBy(item => item));
            AvailableThemes.Insert(0, "<Choose automatically>");
            SelectedTheme = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.SelectedThemeKey) ?? "<Choose automatically>";
            Finished += (o,a) => StoreToRepository();
            vMerge.StudioIntegration.Framework.vMergePackage.MergeToolWindowVisibilityChanged += (o, a) => UpdatePerformNonModalMergeChangeable();
            UpdatePerformNonModalMergeChangeable();
        }

        private void UpdatePerformNonModalMergeChangeable()
        {
            IsPerformNonModalMergeChangeable = !vMerge.StudioIntegration.Framework.vMergePackage.MergeToolWindowIsVisible;
        }
        
        private void LoadFromRepository()
        {
            if (_storing)
                return;
            AutoMergeDirectly = Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.AutoMergeDirectlyKey);
            LinkMergeWithWorkItems = Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.LinkMergeWithWorkItemsKey);
            TempWorkspaceBasePath = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.LocalWorkspaceBasePathKey) ?? Path.GetTempPath();
            CheckInCommentTemplate = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.CheckInCommentTemplateKey);
            HideSplashScreen = Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.HideSplashScreenKey);
            PerformNonModalMerge = Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.PerformNonModalMergeKey);
        }

        private void StoreToRepository()
        {
            _storing = true;
            try
            {
                Repository.Instance.Settings.SetSettings(Constants.Settings.AutoMergeDirectlyKey, AutoMergeDirectly);
                Repository.Instance.Settings.SetSettings(Constants.Settings.LinkMergeWithWorkItemsKey, LinkMergeWithWorkItems);
                Repository.Instance.Settings.SetSettings(Constants.Settings.LocalWorkspaceBasePathKey, TempWorkspaceBasePath);
                Repository.Instance.Settings.SetSettings(Constants.Settings.CheckInCommentTemplateKey, CheckInCommentTemplate);
                Repository.Instance.Settings.SetSettings(Constants.Settings.SelectedThemeKey, SelectedTheme);
                Repository.Instance.Settings.SetSettings(Constants.Settings.HideSplashScreenKey, HideSplashScreen);
                Repository.Instance.Settings.SetSettings(Constants.Settings.PerformNonModalMergeKey, PerformNonModalMerge);
            }
            finally
            {
                _storing = false;
            }
        }

        private void PickTempWorkspacePath()
        {
            string result = Repository.Instance.ViewManager.BrowseForFolder(TempWorkspaceBasePath);
            if (result != null)
                TempWorkspaceBasePath = result;
        }

        private void OpenLogFileFolder()
        {
            if (SimpleLogger.LogFilePath==null)
            {
                var mbvm = new MessageBoxViewModel("Cannot open log file", "The log file could not be openend during startup. Most probably you don't have access rights to the ProgramData folder.", MessageBoxViewModel.MessageBoxButtons.OK);
                Repository.Instance.ViewManager.ShowModal(mbvm);
            }
            else
                Process.Start("explorer.exe", Path.Combine(Path.GetDirectoryName(SimpleLogger.LogFilePath)));
        }

        protected override void SaveInternal(object data)
        {
            return;
        }

        #region IViewModelIsFinishable
        public event EventHandler<ViewModelFinishedEventArgs> Finished;

        public void RaiseFinished(bool success)
        {
            if (Finished != null)
                Finished(this, new ViewModelFinishedEventArgs(success));
            SelectedTheme = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.SelectedThemeKey) ?? "<Choose automatically>";
        }
        #endregion

        void ISettingsChangeListener.SettingsChanged(string key, object data)
        {
            LoadFromRepository();
        }
    }
}
