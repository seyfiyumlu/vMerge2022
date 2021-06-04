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

namespace alexbegh.vMerge.ViewModel.Merge
{
    public class CheckInSummaryViewModel : BaseViewModel, IViewModelIsFinishable
    {
        #region Inner Classes
        public class PendingChangeWithConflict
        {
            public PendingChangeWithConflict(ITfsPendingChange change, ITfsMergeConflict conflict)
            {
                Change = change;
                Conflict = conflict;
            }

            public ITfsPendingChange Change { get; private set; }
            public ITfsMergeConflict Conflict { get; private set; }

            public bool HasConflict { get { return Conflict != null; } }
            public bool HasNoConflict { get { return Conflict == null; } }
            public bool IsEdit { get { return (Change.PendingChange.ChangeType & Microsoft.TeamFoundation.VersionControl.Client.ChangeType.Edit) != 0; } }
            public bool IsDelete { get { return (Change.PendingChange.ChangeType & Microsoft.TeamFoundation.VersionControl.Client.ChangeType.Delete) != 0; } }
            public bool IsAdd { get { return (Change.PendingChange.ChangeType & Microsoft.TeamFoundation.VersionControl.Client.ChangeType.Branch) != 0; } }
        }
        #endregion

        #region Constructor
        public CheckInSummaryViewModel()
            : base(typeof(CheckInSummaryViewModel))
        {
            CheckInCommand = new RelayCommand((o) => CheckIn());
            CancelCommand = new RelayCommand((o) => Cancel());
            DiffToPreviousTargetCommand = new RelayCommand((o) => DiffToPreviousTarget((PendingChangeWithConflict)o), (o) => CanDiffToPreviousTarget((PendingChangeWithConflict)o));
            DiffToPreviousSourceCommand = new RelayCommand((o) => DiffToPreviousSource((PendingChangeWithConflict)o), (o) => CanDiffToPreviousSource((PendingChangeWithConflict)o));
            DiffSourceToTargetCommand = new RelayCommand((o) => DiffSourceToTarget((PendingChangeWithConflict)o), (o) => CanDiffSourceToTarget((PendingChangeWithConflict)o));
            Cancelled = true;
        }
        #endregion

        #region Properties
        private IReadOnlyList<ITfsWorkItem> _associatedWorkItems;
        public IReadOnlyList<ITfsWorkItem> AssociatedWorkItems
        {
            get { return _associatedWorkItems; }
            set { Set(ref _associatedWorkItems, value); }
        }

        private IReadOnlyList<ITfsChangeset> _originalChangesets;
        public IReadOnlyList<ITfsChangeset> OriginalChangesets
        {
            get { return _originalChangesets; }
            set { Set(ref _originalChangesets, value); }
        }

        private IReadOnlyList<ITfsChangeset> _sourceChangesets;
        public IReadOnlyList<ITfsChangeset> SourceChangesets
        {
            get { return _sourceChangesets; }
            set { Set(ref _sourceChangesets, value); }
        }

        private IReadOnlyList<PendingChangeWithConflict> _changes;
        public IReadOnlyList<PendingChangeWithConflict> Changes
        {
            get { return _changes; }
            set { Set(ref _changes, value); }
        }

        private ITfsBranch _sourceBranch;
        public ITfsBranch SourceBranch
        {
            get { return _sourceBranch; }
            set { Set(ref _sourceBranch, value); }
        }

        private ITfsBranch _targetBranch;
        public ITfsBranch TargetBranch
        {
            get { return _targetBranch; }
            set { Set(ref _targetBranch, value); }
        }

        public int ConflictCount
        {
            get { return _changes.Count(change => change.Conflict != null); }
        }

        private string _checkInComment;
        public string CheckInComment
        {
            get { return _checkInComment; }
            set { Set(ref _checkInComment, value); }
        }

        private ITfsTemporaryWorkspace _temporaryWorkspace;
        public ITfsTemporaryWorkspace TemporaryWorkspace
        {
            get { return _temporaryWorkspace; }
            set { Set(ref _temporaryWorkspace, value); }
        }

        private bool _cancelled;
        public bool Cancelled
        {
            get { return _cancelled; }
            set { Set(ref _cancelled, value); }
        }

        private RelayCommand _checkInCommand;
        public RelayCommand CheckInCommand
        {
            get { return _checkInCommand; }
            set { Set(ref _checkInCommand, value); }
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand
        {
            get { return _cancelCommand; }
            set { Set(ref _cancelCommand, value); }
        }

        private RelayCommand _diffToPreviousTargetCommand;
        public RelayCommand DiffToPreviousTargetCommand
        {
            get { return _diffToPreviousTargetCommand; }
            set { Set(ref _diffToPreviousTargetCommand, value); }
        }

        private RelayCommand _diffToPreviousSourceCommand;
        public RelayCommand DiffToPreviousSourceCommand
        {
            get { return _diffToPreviousSourceCommand; }
            set { Set(ref _diffToPreviousSourceCommand, value); }
        }

        private RelayCommand _diffToSourceToTargetCommand;
        public RelayCommand DiffSourceToTargetCommand
        {
            get { return _diffToSourceToTargetCommand; }
            set { Set(ref _diffToSourceToTargetCommand, value); }
        }


        #endregion

        #region Command Handlers
        private void CheckIn()
        {
            Cancelled = false;
            RaiseFinished(true);
        }

        private void Cancel()
        {
            Cancelled = true;
            RaiseFinished(false);
        }

        private void DiffToPreviousTarget(PendingChangeWithConflict pc)
        {
            if (pc == null)
                return;
            if (!Repository.Instance.TfsUIInteractionProvider.ShowDifferencesPerTF(
                TemporaryWorkspace.MappedFolder,
                pc.Change.PendingChange.ServerItem + ";" + pc.Change.PendingChange.SourceVersionFrom.ToString(),
                pc.Change.PendingChange.ServerItem + ";" + (pc.Change.PendingChange.SourceVersionFrom-1).ToString()))
            {
                var mbvm = new MessageBoxViewModel("Cannot show difference", "No differences can be shown.", MessageBoxViewModel.MessageBoxButtons.OK);
                Repository.Instance.ViewManager.ShowModal(mbvm);
            }
        }

        private bool CanDiffToPreviousTarget(PendingChangeWithConflict pc)
        {
            if (pc == null)
                return false;
            if (pc.IsAdd)
                return false;
            return true;
        }

        private void DiffSourceToTarget(PendingChangeWithConflict pc)
        {
            if (pc == null)
                return;
            string exactSource = null;
            var target = pc.Change.ServerPath;
            if (target.StartsWith(TargetBranch.Name, StringComparison.InvariantCultureIgnoreCase))
                target = target.Substring(TargetBranch.Name.Length);

            var candidates
                = SourceChangesets
                    .SelectMany(cs => cs.Changes)
                    .Where(change => change.ServerItem.StartsWith(SourceBranch.Name, StringComparison.InvariantCultureIgnoreCase));

            var fullMatch = candidates.FirstOrDefault(candidate => candidate.ServerItem.Substring(SourceBranch.Name.Length) == target);
            if (fullMatch != null)
            {
                exactSource = fullMatch.ServerItem;
            }

            if (exactSource!=null)
            {
                Repository.Instance.TfsUIInteractionProvider.ShowDifferencesPerTF(
                    TemporaryWorkspace.MappedFolder,
                    exactSource,
                    pc.Change.PendingChange.LocalItem);
            }
        }

        private bool CanDiffSourceToTarget(PendingChangeWithConflict pc)
        {
            if (pc == null)
                return false;
            if (!pc.IsEdit)
                return false;
            return true;
        }

        private void DiffToPreviousSource(PendingChangeWithConflict pc)
        {
            if (pc == null)
                return;
            string exactSource = null;
            var target = pc.Change.ServerPath;
            if (target.StartsWith(TargetBranch.Name, StringComparison.InvariantCultureIgnoreCase))
                target = target.Substring(TargetBranch.Name.Length);

            var candidates
                = SourceChangesets
                    .SelectMany(cs => cs.Changes)
                    .Where(change => change.ServerItem.StartsWith(SourceBranch.Name, StringComparison.InvariantCultureIgnoreCase));

            var fullMatch = candidates.FirstOrDefault(candidate => candidate.ServerItem.Substring(SourceBranch.Name.Length) == target);
            if (fullMatch != null)
            {
                exactSource = fullMatch.ServerItem;
            }

            if (exactSource != null && (fullMatch.Change.Item.ChangesetId > 1))
            {
                Repository.Instance.TfsUIInteractionProvider.ShowDifferencesPerTF(
                    TemporaryWorkspace.MappedFolder,
                    exactSource + ";" + (fullMatch.Change.Item.ChangesetId - 1).ToString(),
                    exactSource + ";" + fullMatch.Change.Item.ChangesetId.ToString());
            }
        }

        private bool CanDiffToPreviousSource(PendingChangeWithConflict pc)
        {
            if (pc == null)
                return false;
            if (pc.IsDelete)
                return false;
            return true;
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
