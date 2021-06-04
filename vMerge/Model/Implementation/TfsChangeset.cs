using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Implementation
{
    class TfsChangeset : ITfsChangeset
    {
        #region Properties
        /// <summary>
        /// Private lock target
        /// </summary>
        private object _locker = new object();

        /// <summary>
        /// 
        /// </summary>
        public Changeset Changeset
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public int Id
        {
            get;
            private set;
        }

        public string CheckedInBy
        {
            get;
            private set;
        }

        public bool HasChangesLoaded
        {
            get { return (Changeset != null) ? (Changeset.Changes.Length != 0) : false; }
        }

        private List<ITfsWorkItem> _relatedWorkItems;
        public IReadOnlyList<ITfsWorkItem> RelatedWorkItems
        {
            get
            {
                var rwi = _relatedWorkItems;
                if (rwi != null)
                    return rwi;
                lock (_locker)
                {
                    if (_relatedWorkItems != null)
                        return _relatedWorkItems;
                    _relatedWorkItems = new List<ITfsWorkItem>(Changeset.WorkItems.Select(workItem => new TfsWorkItem(workItem)));
                    return _relatedWorkItems;
                }
            }
        }

        private List<ITfsChangeset> _sourceChangesets;
        public IReadOnlyList<ITfsChangeset> SourceChangesets
        {
            get
            {
                var scs = _sourceChangesets;
                if (scs != null)
                    return scs;
                lock (_locker)
                {
                    if (_sourceChangesets != null)
                        return _sourceChangesets;
                    _sourceChangesets = Repository.Instance.TfsBridgeProvider.GetMergeSourceChangesets(this).ToList();
                    return _sourceChangesets;
                }
            }
        }

        private List<ITfsChange> _changes;
        public IReadOnlyList<ITfsChange> Changes
        {
            get
            {
                if (_changes == null)
                {
                    _changes = new List<ITfsChange>();
                    if (Changeset.Changes.Length==0)
                    {
                        Changeset = Repository.Instance.TfsBridgeProvider.VersionControlServer.GetChangeset(Changeset.ChangesetId, true, false);
                    }
                    foreach (var change in Changeset.Changes)
                    {
                        _changes.Add(new TfsChange(change));
                    }
                }
                return _changes;
            }
        }

        private Dictionary<string, List<ITfsChange>> _changesPerBranch;
        private void BuildChangePerBranchDictionary()
        {
            if (_changesPerBranch == null)
            {
                _changesPerBranch = new Dictionary<string, List<ITfsChange>>();

                foreach (var change in Changes.Where(change => change.RootBranch!=null))
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

        public IEnumerable<MergedChangeset> FindMergesForActiveProject(ITfsBranch source, IEnumerable<ITfsBranch> potentialMergeSourceBranches = null)
        {
            var branches = GetAffectedBranchesForActiveProject().ToList();
            if (!branches.Contains(source))
                yield break;
            if (potentialMergeSourceBranches == null)
                potentialMergeSourceBranches = branches;

            ExtendedMerge[] results = null;
            try
            {
                results =
                    Repository.Instance.TfsBridgeProvider.VersionControlServer
                        .TrackMerges(
                            new int[] { Changeset.ChangesetId },
                            source.BranchObject.Properties.RootItem,
                            potentialMergeSourceBranches.Select(branch => branch.BranchObject.Properties.RootItem).ToArray(),
                            null);
            }
            catch(Exception)
            {
                yield break;
            }

            var tempDictionary = new Dictionary<int, ITfsChangeset>();

            foreach (var item in results)
            {
                ITfsChangeset sourceCS, targetCS;
                if (tempDictionary.TryGetValue(item.SourceChangeset.ChangesetId, out sourceCS) == false)
                {
                    sourceCS = new TfsChangeset(Repository.Instance.TfsBridgeProvider.VersionControlServer.GetChangeset(item.SourceChangeset.ChangesetId));
                    tempDictionary[item.SourceChangeset.ChangesetId] = sourceCS;
                }
                if (tempDictionary.TryGetValue(item.TargetChangeset.ChangesetId, out targetCS) == false)
                {
                    targetCS = new TfsChangeset(Repository.Instance.TfsBridgeProvider.VersionControlServer.GetChangeset(item.TargetChangeset.ChangesetId));
                    tempDictionary[item.TargetChangeset.ChangesetId] = targetCS;
                }

                yield return new MergedChangeset()
                {
                    Source = sourceCS,
                    Target = targetCS
                };
            }
        }


        #endregion

        #region Constructor
        internal TfsChangeset(Changeset changeset)
        {
            Changeset = changeset;
            Id = Changeset.ChangesetId;
            CheckedInBy = Changeset.CommitterDisplayName;
            Description = Changeset.Comment;
        }
        #endregion

        #region Operations
        public ITfsChangeset HasBeenMergedInto(ITfsBranch target)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

