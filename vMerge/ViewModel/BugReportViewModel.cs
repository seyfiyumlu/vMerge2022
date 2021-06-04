using alexbegh.Utility.Commands;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.ViewModel
{
    public class BugReportViewModel : BaseViewModel, IViewModelIsFinishable
    {
        #region Constructor
        public BugReportViewModel()
            : base(typeof(BugReportViewModel))
        {
            Cancelled = true;
            SubmitCommand = new RelayCommand((o) => Submit(), (o) => BugLocation != null && Problem != null && Reproducability != null && UsabilityImpact != null && Description != null && Description.Length != 0);
            CancelCommand = new RelayCommand((o) => Cancel());
            BugLocations = new string[] {
                "On startup of visual studio",
                "On opening of the work item view",
                "On opening of the changeset view",
                "On opening the options dialog pages",
                "On closing visual studio",
                "On performing a merge in the merge dialog",
                "On performing a merge in the check-in summary dialog",
                "On performing an automerge",
                "During selection of branches in the changeset view",
                "During selection of branches in the work item view",
                "During selection of a query in the changeset view",
                "During selection of a query in the work item view",
                "Other (please state in description)"
            }.ToList();

            Problems = new string[] {
                "I can't view changesets",
                "I can't view work items",
                "Slow performance in absolute terms",
                "Slow performance in comparison (please state in description)",
                "I couldn't merge",
                "I couldn't select what i wanted",
                "An exception occurred",
                "Visual Studio closed unexpectedly",
                "I expect different results",
                "Other (please state in description)"
            }.ToList();

            Reproducabilities = new string[] {
                "Happens always",
                "Happens most of the time",
                "Happens sometimes",
                "Happens rarely",
                "Happens sometimes"
            }.ToList();

            UsabilityImpacts = new string[] {
                "Hardly impacted",
                "Impacted but usable",
                "Severely impacted",
                "Unusable"
            }.ToList();



        }
        #endregion

        #region Public Properties
        private RelayCommand _submitCommand;
        public RelayCommand SubmitCommand
        {
            get { return _submitCommand; }
            set { Set(ref _submitCommand, value); }
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand
        {
            get { return _cancelCommand; }
            set { Set(ref _cancelCommand, value); }
        }

        public List<string> BugLocations { get; private set; }

        public List<string> Problems { get; private set; }

        public List<string> Reproducabilities { get; private set; }

        public List<string> UsabilityImpacts { get; private set; }

        private string _bugLocation;
        public string BugLocation
        { 
            get { return _bugLocation; }
            set { Set(ref _bugLocation, value); }
        }

        private string _problem;
        public string Problem
        {
            get { return _problem; }
            set { Set(ref _problem, value); }
        }

        private string _reproducability;
        public string Reproducability
        {
            get { return _reproducability; }
            set { Set(ref _reproducability, value); }
        }

        private string _usabilityImpact;
        public string UsabilityImpact
        {
            get { return _usabilityImpact; }
            set { Set(ref _usabilityImpact, value); }
        }

        private string _email;
        public string EMail
        {
            get { return _email; }
            set { Set(ref _email, value); }
        }

        public byte[] Description { get; set; }

        public bool Cancelled { get; private set; }
        #endregion

        #region Command Handlers
        public void Submit()
        {
            Cancelled = false;
            RaiseFinished(true);
        }


        public void Cancel()
        {
            RaiseFinished(false);
        }
        #endregion

        #region Overrides
        protected override void SaveInternal(object data)
        {
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
    }
}
