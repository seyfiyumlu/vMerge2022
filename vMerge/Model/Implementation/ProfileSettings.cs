using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.Model.Implementation
{
    [RegisterForSerialization]
    [Serializable]
    public class ProfileSettings : NotifyPropertyChangedImpl, IProfileSettings
    {
        public ProfileSettings(string teamProject, string teamProjectFriendlyName, string name, Action<ProfileSettings> setDirty)
            : base(typeof(ProfileSettings))
        {
            SetDirty = setDirty;
            Name = name;
            TeamProject = teamProject;
            TeamProjectFriendlyName = teamProjectFriendlyName;
        }

        public ProfileSettings()
            : base(typeof(ProfileSettings))
        {
            SetDirty = Repository.Instance.ProfileProvider.SetProfileDirty;
        }
        
        private Action<ProfileSettings> SetDirty
        {
            get;
            set;
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        private string _teamProject;
        public string TeamProject
        {
            get { return _teamProject; }
            set { Set(ref _teamProject, value); }
        }

        private string _teamProjectFriendlyName;
        public string TeamProjectFriendlyName
        {
            get { return _teamProjectFriendlyName; }
            set { Set(ref _teamProjectFriendlyName, value); }
        }

        private string _wiSourceBranch;
        public string WISourceBranch
        {
            get { return _wiSourceBranch; }
            set { Set(ref _wiSourceBranch, value, () => SetDirty(this)); }
        }

        private string _wiTargetBranch;
        public string WITargetBranch
        {
            get { return _wiTargetBranch; }
            set { Set(ref _wiTargetBranch, value, () => SetDirty(this)); }
        }

        private string _wiSourcePathFilter;
        public string WISourcePathFilter
        {
            get { return _wiSourcePathFilter; }
            set { Set(ref _wiSourcePathFilter, value, () => SetDirty(this)); }
        }

        private string _wiQueryName;
        public string WIQueryName
        {
            get { return _wiQueryName; }
            set { Set(ref _wiQueryName, value, () => SetDirty(this)); }
        }

        private string _csSourceBranch;
        public string CSSourceBranch
        {
            get { return _csSourceBranch; }
            set { Set(ref _csSourceBranch, value, () => SetDirty(this)); }
        }

        private string _csTargetBranch;
        public string CSTargetBranch
        {
            get { return _csTargetBranch; }
            set { Set(ref _csTargetBranch, value, () => SetDirty(this)); }
        }

        private string _csSourcePathFilter;
        public string CSSourcePathFilter
        {
            get { return _csSourcePathFilter; }
            set { Set(ref _csSourcePathFilter, value, () => SetDirty(this)); }
        }

        private string _csQueryName;
        public string CSQueryName
        {
            get { return _csQueryName; }
            set { Set(ref _csQueryName, value, () => SetDirty(this)); }
        }

        private string _changesetIncludeCommentFilter;
        public string ChangesetIncludeCommentFilter
        {
            get { return _changesetIncludeCommentFilter; }
            set { Set(ref _changesetIncludeCommentFilter, value, () => SetDirty(this)); }
        }

        private string _changesetExcludeCommentFilter;
        public string ChangesetExcludeCommentFilter
        {
            get { return _changesetExcludeCommentFilter; }
            set { Set(ref _changesetExcludeCommentFilter, value, () => SetDirty(this)); }
        }

        private DateTime? _dateFromFilter;
        public DateTime? DateFromFilter
        {
            get { return _dateFromFilter; }
            set { Set(ref _dateFromFilter, value, () => SetDirty(this)); }
        }

        private DateTime? _dateToFilter;
        public DateTime? DateToFilter
        {
            get { return _dateToFilter; }
            set { Set(ref _dateToFilter, value, () => SetDirty(this)); }
        }

        private string _includeByUserFilterActive;
        public string ChangesetIncludeUserFilter
        {
            get { return _includeByUserFilterActive; }
            set { Set(ref _includeByUserFilterActive, value, () => SetDirty(this)); }
        }

        public void CopyTo(IProfileSettings other)
        {
            other.ChangesetIncludeCommentFilter = ChangesetIncludeCommentFilter;
            other.ChangesetExcludeCommentFilter = ChangesetExcludeCommentFilter;
            other.CSQueryName = CSQueryName;
            other.CSSourceBranch = CSSourceBranch;
            other.CSTargetBranch = CSTargetBranch;
            other.CSSourcePathFilter = CSSourceBranch;
            other.DateFromFilter = DateFromFilter;
            other.DateToFilter = DateToFilter;
            other.WIQueryName = WIQueryName;
            other.WISourceBranch = WISourceBranch;
            other.WISourcePathFilter = WISourcePathFilter;
            other.WITargetBranch = WITargetBranch;
        }

        public override bool Equals(object obj)
        {
            IProfileSettings other = obj as IProfileSettings;
            if (other == null)
                return false;

            if (DateFromFilter.HasValue != other.DateFromFilter.HasValue)
                return false;
            if (DateToFilter.HasValue != other.DateToFilter.HasValue)
                return false;
            if (DateFromFilter.HasValue && DateFromFilter.Value != other.DateFromFilter.Value)
                return false;
            if (DateToFilter.HasValue && DateToFilter.Value != other.DateToFilter.Value)
                return false;

            return
                other.ChangesetIncludeCommentFilter == ChangesetIncludeCommentFilter &&
                other.ChangesetExcludeCommentFilter == ChangesetExcludeCommentFilter &&
                other.CSQueryName == CSQueryName &&
                other.CSSourceBranch == CSSourceBranch &&
                other.CSTargetBranch == CSTargetBranch &&
                other.CSSourcePathFilter == CSSourceBranch &&
                other.WIQueryName == WIQueryName &&
                other.WISourceBranch == WISourceBranch &&
                other.WISourcePathFilter == WISourcePathFilter &&
                other.WITargetBranch == WITargetBranch;
        }

        public override int GetHashCode()
        {
            return new
            {
                DateFromFilter,
                DateToFilter,
                ChangesetExcludeCommentFilter,
                ChangesetIncludeCommentFilter,
                CSQueryName,
                CSSourceBranch,
                CSTargetBranch,
                WIQueryName,
                WISourceBranch,
                WISourcePathFilter,
                WITargetBranch
            }.GetHashCode();
        }
    }
}
