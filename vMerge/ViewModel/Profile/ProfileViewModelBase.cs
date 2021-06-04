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
    public abstract class ProfileViewModelBase : BaseViewModel
    {
        #region Static Constructor
        static ProfileViewModelBase()
        {
        }
        #endregion

        #region Constructors
        public ProfileViewModelBase(Type callerType, IProfileSettings profile)
            : base(callerType)
        {
            var tfsUri = profile.TeamProject;
            var tfsProject = profile.TeamProjectFriendlyName;

            TfsUri = tfsUri;
            TfsProject = tfsProject;

            ChangesetSelectionType = String.IsNullOrEmpty(profile.CSQueryName) ? "Merge candidates" : "Query";
            ChangesetSourceBranch = profile.CSSourceBranch;
            ChangesetTargetBranch = profile.CSTargetBranch;
            ChangesetQuery = profile.CSQueryName;
            ChangesetIncludeFilterRegEx = String.IsNullOrEmpty(profile.ChangesetIncludeCommentFilter) ? "<not set>" : profile.ChangesetIncludeCommentFilter;
            ChangesetExcludeFilterRegEx = String.IsNullOrEmpty(profile.ChangesetExcludeCommentFilter) ? "<not set>" : profile.ChangesetExcludeCommentFilter;
            ChangesetFilterMinDate = (profile.DateFromFilter != null) ? profile.DateFromFilter.Value.ToString(CultureInfo.InvariantCulture.DateTimeFormat) : "<not set>";
            ChangesetFilterMaxDate = (profile.DateToFilter != null) ? profile.DateToFilter.Value.ToString(CultureInfo.InvariantCulture.DateTimeFormat) : "<not set>";
            WorkItemSelectionType = String.IsNullOrEmpty(profile.WIQueryName) ? "Merge candidates" : "Query";
            WorkItemSourceBranch = profile.WISourceBranch;
            WorkItemTargetBranch = profile.WITargetBranch;
            WorkItemQuery = profile.WIQueryName;
        }
        #endregion

        #region Public Properties
        private string _tfsUri;
        public string TfsUri
        {
            get { return _tfsUri; }
            set { Set(ref _tfsUri, value); }
        }

        private string _tfsProject;
        public string TfsProject
        {
            get { return _tfsProject; }
            set { Set(ref _tfsProject, value); }
        }

        private string _changesetSelectionType;
        public string ChangesetSelectionType
        {
            get { return _changesetSelectionType; }
            set { Set(ref _changesetSelectionType, value); }
        }

        private string _changesetSourceBranch;
        public string ChangesetSourceBranch
        {
            get { return _changesetSourceBranch; }
            set { Set(ref _changesetSourceBranch, value); }
        }

        private string _changesetTargetBranch;
        public string ChangesetTargetBranch
        {
            get { return _changesetTargetBranch; }
            set { Set(ref _changesetTargetBranch, value); }
        }

        private string _changesetQuery;
        public string ChangesetQuery
        {
            get { return _changesetQuery; }
            set { Set(ref _changesetQuery, value); }
        }

        private string _changesetFilterIsActive;
        public string ChangesetFilterIsActive
        {
            get { return _changesetFilterIsActive; }
            set { Set(ref _changesetFilterIsActive, value); }
        }

        private string _changesetIncludeFilterRegEx;
        public string ChangesetIncludeFilterRegEx
        {
            get { return _changesetIncludeFilterRegEx; }
            set { Set(ref _changesetIncludeFilterRegEx, value); }
        }

        private string _changesetExcludeFilterRegEx;
        public string ChangesetExcludeFilterRegEx
        {
            get { return _changesetExcludeFilterRegEx; }
            set { Set(ref _changesetExcludeFilterRegEx, value); }
        }

        private string _changesetFilterMinDate;
        public string ChangesetFilterMinDate
        {
            get { return _changesetFilterMinDate; }
            set { Set(ref _changesetFilterMinDate, value); }
        }

        private string _changesetFilterMaxDate;
        public string ChangesetFilterMaxDate
        {
            get { return _changesetFilterMaxDate; }
            set { Set(ref _changesetFilterMaxDate, value); }
        }

        private string _workItemSelectionType;
        public string WorkItemSelectionType
        {
            get { return _workItemSelectionType; }
            set { Set(ref _workItemSelectionType, value); }
        }

        private string _workItemSourceBranch;
        public string WorkItemSourceBranch
        {
            get { return _workItemSourceBranch; }
            set { Set(ref _workItemSourceBranch, value); }
        }

        private string _workItemTargetBranch;
        public string WorkItemTargetBranch
        {
            get { return _workItemTargetBranch; }
            set { Set(ref _workItemTargetBranch, value); }
        }

        private string _workItemQuery;
        public string WorkItemQuery
        {
            get { return _workItemQuery; }
            set { Set(ref _workItemQuery, value); }
        }
        #endregion
    }
}
