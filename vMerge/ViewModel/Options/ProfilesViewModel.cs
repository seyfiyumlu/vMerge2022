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
using System.Collections.ObjectModel;
using alexbegh.vMerge.Model.Interfaces;
using alexbegh.vMerge.ViewModel.Profile;

namespace alexbegh.vMerge.ViewModel.Options
{
    class ProfilesViewModel : BaseViewModel, IViewModelIsFinishable
    {
        private ObservableCollection<IProfileSettings> _profiles;
        public ObservableCollection<IProfileSettings> Profiles
        {
            get { return _profiles; }
            set { Set(ref _profiles, value); }
        }

        private RelayCommand _viewProfileCommand;
        public RelayCommand ViewProfileCommand
        {
            get { return _viewProfileCommand; }
            set { Set(ref _viewProfileCommand, value); }
        }

        private RelayCommand _deleteProfileCommand;
        public RelayCommand DeleteProfileCommand
        {
            get { return _deleteProfileCommand; }
            set { Set(ref _deleteProfileCommand, value); }
        }

        public bool HasProfiles
        {
            get { return _profiles != null && _profiles.Count > 0; }
        }

        static ProfilesViewModel()
        {
            AddDependency<ProfilesViewModel>("Profiles", "HasProfiles");
        }

        public ProfilesViewModel()
            : base(typeof(ProfilesViewModel))
        {
            Finished += (o,a) => StoreToRepository();
            Profiles = new ObservableCollection<IProfileSettings>(Repository.Instance.ProfileProvider.GetAllProfiles());
            ViewProfileCommand = new RelayCommand((o) => ViewProfile(o as IProfileSettings));
            DeleteProfileCommand = new RelayCommand((o) => DeleteProfile(o as IProfileSettings));

            Repository.Instance.ProfileProvider.ProfilesChanged += (o, a) => Profiles = new ObservableCollection<IProfileSettings>(Repository.Instance.ProfileProvider.GetAllProfiles());
        }

        private void ViewProfile(IProfileSettings profileSettings)
        {
            var vm = new ViewProfileViewModel(profileSettings);
            var dlg = Repository.Instance.ViewManager.ShowModal(vm);
        }

        private void DeleteProfile(IProfileSettings profileSettings)
        {
            MessageBoxViewModel mbvm = new MessageBoxViewModel("Delete profile?", String.Format("You are about to delete the profile '{0}'. This action cannot be undone.\r\nProceed?", profileSettings.Name), MessageBoxViewModel.MessageBoxButtons.None);
            var yesButton = new MessageBoxViewModel.MessageBoxButton("_Yes");
            mbvm.ConfirmButtons.Add(yesButton);
            mbvm.ConfirmButtons.Add(new MessageBoxViewModel.MessageBoxButton("_No"));
            Repository.Instance.ViewManager.ShowModal(mbvm);

            if (yesButton.IsChecked)
            {
                if (Repository.Instance.ProfileProvider.DeleteProfile(profileSettings))
                {
                    Profiles.Remove(profileSettings);
                }
            }
        }

        private void StoreToRepository()
        {
            //Repository.Instance.Settings.SetSettings(Constants.Settings.ShowConfirmationDialogKey, ShowConfirmationDialog);
            //Repository.Instance.Settings.SetSettings(Constants.Settings.LinkMergeWithWorkItemsKey, LinkMergeWithWorkItems);
            //Repository.Instance.Settings.SetSettings(Constants.Settings.LocalWorkspaceBasePathKey, TempWorkspaceBasePath);
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
        }
        #endregion
    }
}
