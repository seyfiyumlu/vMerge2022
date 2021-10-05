// Guids.cs
// MUST match guids.h
using System;

namespace alexbegh.vMerge.StudioIntegration.Framework
{
    static partial class GuidList
    {
        public const string guidvMergePkgString = "DD29E551-59CD-4353-9B69-CBDDAFB09756";

        public const string guidWorkItemsToolWindowPersistanceString = "ec10ec1c-57aa-4402-8e88-ed7a2b73ae4a";
        public const string guidChangesetsToolWindowPersistanceString = "ec10ec1c-57aa-4402-8e88-ed7a2b73ae4b";

        public const string guidVMergeMenuCmdSetString = "73CE7046-6F54-44B7-B84C-67CCA8DB44F5";
        public const string guidVMergeTeamExplorerMenuCmdSetString = "73CE8346-6F54-44B7-B84C-67CCA8DB44F5";

        public static readonly Guid guidVMergeMenuCmdSet = new Guid(guidVMergeMenuCmdSetString);
        public static readonly Guid guidVMergeTeamExplorerMenuCmdSet = new Guid(guidVMergeTeamExplorerMenuCmdSetString);
    };
}