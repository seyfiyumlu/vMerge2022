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
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.ViewModel.Profile
{
    public class ViewProfileViewModel : ProfileViewModelBase, IViewModelIsFinishable
    {
        #region Static Constructor
        static ViewProfileViewModel()
        {
        }
        #endregion

        #region Constructors
        public ViewProfileViewModel(IProfileSettings profile)
            : base(typeof(ViewProfileViewModel), profile)
        {
            OKCommand = new RelayCommand((o) => OK());
        }
        #endregion

        #region Public Properties
        private RelayCommand _okCommand;
        public RelayCommand OKCommand
        {
            get { return _okCommand; }
            set { Set(ref _okCommand, value); }
        }
        #endregion

        #region Command Handlers
        void OK()
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
