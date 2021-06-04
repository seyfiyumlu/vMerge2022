using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alexbegh.Utility.Commands;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.Model;

namespace alexbegh.vMerge.ViewModel.Profile
{
    public class SaveProfileAsViewModel : ProfileViewModelBase, IViewModelIsFinishable
    {
        #region Static Constructor
        static SaveProfileAsViewModel()
        {
        }
        #endregion

        #region Constructors
        public SaveProfileAsViewModel()
            : base(typeof(SaveProfileAsViewModel), Repository.Instance.ProfileProvider.GetDefaultProfile())
        {
            ExistingProfileNames = new ObservableCollection<string>(Repository.Instance.ProfileProvider.GetAllProfilesForProject().Select(prf => prf.Name));
            ProfileName = "";
            SaveCommand = new RelayCommand((o) => Save());
            CancelCommand = new RelayCommand((o) => Cancel());
        }
        #endregion

        #region Public Properties
        private ObservableCollection<string> _existingProfileNames;
        public ObservableCollection<string> ExistingProfileNames
        {
            get { return _existingProfileNames; }
            set { Set(ref _existingProfileNames, value); }
        }

        private string _profileName;
        public string ProfileName
        {
            get { return _profileName; }
            set { Set(ref _profileName, value); }
        }

        private RelayCommand _saveCommand;
        public RelayCommand SaveCommand
        {
            get { return _saveCommand; }
            set { Set(ref _saveCommand, value); }
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand
        {
            get { return _cancelCommand; }
            set { Set(ref _cancelCommand, value); }
        }
        #endregion

        #region Command Handlers
        void Save()
        {
            if (!Repository.Instance.ProfileProvider.SaveProfileAs(null, ProfileName, false))
            {
                var mbvm = new MessageBoxViewModel("Profile already exists", "The selected profile '" + ProfileName + "' already exists. Overwrite?", MessageBoxViewModel.MessageBoxButtons.None);
                var yes = new MessageBoxViewModel.MessageBoxButton("Overwrite");
                var no = new MessageBoxViewModel.MessageBoxButton("Cancel");
                mbvm.ConfirmButtons.Add(yes);
                mbvm.ConfirmButtons.Add(no);
                Repository.Instance.ViewManager.ShowModal(mbvm);
                if (yes.IsChecked)
                    Repository.Instance.ProfileProvider.SaveProfileAs(null, ProfileName, true);
                else
                    return;
            }
            RaiseFinished(true);
        }

        void Cancel()
        {
            RaiseFinished(false);
        }
        #endregion

        #region IViewModelIsFinishable
        public event EventHandler<ViewModelFinishedEventArgs> Finished;

        public void RaiseFinished(bool success)
        {
            if (Finished != null)
                Finished(this, new ViewModelFinishedEventArgs(success));
        }
        #endregion

        #region Abstract Methods Overrides
        protected override void SaveInternal(object data)
        {
        }
        #endregion
    }
}
