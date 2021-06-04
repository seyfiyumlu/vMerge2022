using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Constants
{
    public static class Tasks
    {
        /* TfsBridgeProvider */
        public const string LoadCompleteBranchListTaskKey = "LoadCompleteBranchList";
        public const string LoadRootQueryTaskKey = "LoadRootQuery";
        public const string CheckTfsUserTaskKey = "CheckTfsUser";

        /* ViewModel */
        public const string LoadSourceBranchesTaskKey = "LoadSourceBranches";
        public const string LoadChangesetsTaskKey = "LoadChangesets";
        public const string LoadWorkItemsTaskKey = "LoadWorkItems";

        public const string LoadAllAssociatedChangesetsIncludingMergesKey = "LoadAllAssociatedChangesetsIncludingMerges";
        public const string LoadChangesetListKey = "LoadChangesetList";

        /* Model */
        public const string PendingChangeRefreshTaskKey = "LoadPendingChanges";
        public const string ConflictsRefreshTaskKey = "LoadConflict";
        public const string RefreshTaskKey = "Refresh";
    }
}
