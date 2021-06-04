using Microsoft.TeamFoundation.VersionControl.Client;
using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace alexbegh.vMerge.Model.Implementation
{
    [Serializable]
    [RegisterForSerialization]
    public class TfsBranch : ITfsBranch
    {
        [NonSerialized]
        private Microsoft.TeamFoundation.VersionControl.Client.VersionControlServer _vcs;
        [XmlIgnore]
        public Microsoft.TeamFoundation.VersionControl.Client.VersionControlServer Vcs
        {
            get
            {
                return _vcs;
            }
            set
            {
                _vcs = value;
            }
        }

        [NonSerialized]
        private Microsoft.TeamFoundation.VersionControl.Client.BranchObject _branchObject;
        [XmlIgnore]
        public Microsoft.TeamFoundation.VersionControl.Client.BranchObject BranchObject
        {
            get
            {
                if (_branchObject == null && !IsSubBranch)
                {
                    var result = _vcs.QueryBranchObjects(new Microsoft.TeamFoundation.VersionControl.Client.ItemIdentifier(Name), Microsoft.TeamFoundation.VersionControl.Client.RecursionType.None);
                    if (result.Length == 0)
                        throw new InvalidOperationException("Couldn't find branch object for server item " + Name);
                    if (result.Length > 1)
                        throw new InvalidOperationException("Found multiple branch objects for server item " + Name);
                    _branchObject = result[0];
                }
                return _branchObject;
            }
            set
            {
                _branchObject = value;
            }
        }

        public string Name
        {
            get;
            set;
        }

        public string ServerPath
        {
            get
            {
                return Name;
            }
        }

        private bool _isSubBranch;
        public bool IsSubBranch
        {
            get
            {
                return _isSubBranch;
            }
            set
            {
                _isSubBranch = value;
            }
        }

        [NonSerialized]
        private List<string> _childBranchNames;
        public List<string> ChildBranchNames
        {
            get
            {
                if (_childBranchNames == null && _vcs != null && !IsSubBranch)
                {
                    _childBranchNames = BranchObject.ChildBranches.Select(cb => cb.Item).ToList();
                }
                else if (_childBranches==null && _vcs != null && IsSubBranch)
                {
                    _childBranchNames = ChildBranches != null ? ChildBranches.Select(br => br.ServerPath).ToList() : null;
                }
                return _childBranchNames;
            }
            set
            {
                _childBranchNames = value;
            }
        }

        [XmlIgnore]
        private List<ITfsBranch> _childBranches;
        [XmlIgnore]
        public List<ITfsBranch> ChildBranches
        {
            get
            {
                if (_childBranches == null && _vcs != null && IsSubBranch)
                {
                    _childBranches = GetChildBranchesForSpecificSubBranch().ToList();
                }
                else if (!IsSubBranch)
                {
                    _childBranches = new List<ITfsBranch>();
                }
                return _childBranches;
            }
            private set
            {
                _childBranches = value;
            }
        }

        internal TfsBranch(VersionControlServer vcs, Microsoft.TeamFoundation.VersionControl.Client.BranchObject branchObject)
        {
            _vcs = vcs;
            BranchObject = branchObject;
            Name = BranchObject == null ? null : BranchObject.Properties.RootItem.Item;
        }

        internal TfsBranch(VersionControlServer vcs, string name)
        {
            _vcs = vcs;
            Name = name;
        }

        internal TfsBranch(VersionControlServer vcs, string name, bool isSubBranch)
        {
            _vcs = vcs;
            Name = name;
            _isSubBranch = isSubBranch;
        }

        internal TfsBranch()
        {

        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
                return true;

            if (!(obj is ITfsBranch))
                return false;

            return ((ITfsBranch)obj).Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        private IEnumerable<ITfsBranch> GetChildBranchesForSpecificSubBranch()
        {
            var mergeRelationships = _vcs.QueryMergeRelationships(ServerPath);
            return mergeRelationships
                .Where(mr => !mr.IsDeleted && !String.IsNullOrWhiteSpace(mr.Item))
                .Select(mr => new TfsBranch(_vcs, mr.Item, true));
        }
    }
}
