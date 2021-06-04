using Microsoft.TeamFoundation.VersionControl.Client;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Implementation
{
    public class TfsChange : ITfsChange
    {
        private bool _rootBranchFetched;
        private ITfsBranch _rootBranch;
        public ITfsBranch RootBranch
        {
            get 
            {
                if (!_rootBranchFetched)
                {
                    _rootBranchFetched = true;
                    _rootBranch
                        = Repository.Instance.TfsBridgeProvider.CompleteBranchList.Where(
                            branch => ServerItem.StartsWith(branch.Name, StringComparison.InvariantCultureIgnoreCase))
                            .OrderByDescending(branch => branch.Name.Length)
                            .FirstOrDefault();
                }
                return _rootBranch;
            }
        }

        public Change Change
        {
            get;
            private set;
        }

        public string ServerItem
        {
            get
            {
                return Change.Item.ServerItem;
            }
        }

        internal TfsChange(Change change)
        {
            Change = change;
        }
    }
}
