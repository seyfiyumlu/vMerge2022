using Microsoft.TeamFoundation.WorkItemTracking.Client;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Implementation
{
    class TfsWorkItem : ITfsWorkItem
    {
        public WorkItem WorkItem
        {
            get;
            private set;
        }

        public int Id
        {
            get { return WorkItem.Id; }
        }

        public DateTime ChangedDate
        {
            get { return WorkItem.ChangedDate; }
        }

        public string Title
        {
            get { return WorkItem.Title; }
        }

        public int RelatedChangesetCount
        {
            get { return Repository.Instance.TfsBridgeProvider.GetRelatedChangesetsForWorkItemCount(this); }
        }

        private List<ITfsChangeset> _relatedChangesets;
        public List<ITfsChangeset> RelatedChangesets
        {
            get
            {
                if (_relatedChangesets == null)
                {
                    _relatedChangesets = new List<ITfsChangeset>(Repository.Instance.TfsBridgeProvider.GetRelatedChangesetsForWorkItem(this));
                }
                return _relatedChangesets;
            }
        }

        private Dictionary<string, List<ITfsChange>> _changesPerBranch;
        private void BuildChangePerBranchDictionary()
        {
            if (_changesPerBranch == null)
            {
                _changesPerBranch = new Dictionary<string, List<ITfsChange>>();
                foreach (var changeset in RelatedChangesets)
                {
                    foreach (var change in changeset.Changes)
                    {
                        List<ITfsChange> listToAddTo = null;
                        if (_changesPerBranch.TryGetValue(change.RootBranch.Name, out listToAddTo) == false)
                        {
                            listToAddTo = new List<ITfsChange>();
                            _changesPerBranch[change.RootBranch.Name] = listToAddTo;
                        }
                        listToAddTo.Add(change);
                    }
                }
            }
        }

        public IEnumerable<ITfsBranch> GetAffectedBranchesForActiveProject()
        {
            BuildChangePerBranchDictionary();
            foreach (var name in _changesPerBranch.Keys)
            {
                var foundBranch = Repository.Instance.TfsBridgeProvider.CompleteBranchList.Where(branch => branch.Name == name).FirstOrDefault();
                if (foundBranch != null)
                    yield return foundBranch;
            }
        }

        public IReadOnlyList<ITfsChange> GetChangesForBranch(ITfsBranch branch)
        {
            BuildChangePerBranchDictionary();
            List<ITfsChange> changes;
            if (_changesPerBranch.TryGetValue(branch.Name, out changes) == false)
                return null;
            return changes;
        }

        public IEnumerable<ITfsChange> GetAllChanges()
        {
            BuildChangePerBranchDictionary();
            foreach (var changeList in _changesPerBranch.Values)
                foreach (var change in changeList)
                    yield return change;
        }

        internal TfsWorkItem(WorkItem source)
        {
            WorkItem = source;
        }
    }
}
