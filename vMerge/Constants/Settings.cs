using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Constants
{
    public static class Settings
    {
        public const string ChangesetColumnSettingsKey = "ChangesetsView";
        public const string WorkItemColumnSettingsKey = "WorkItemsView";

        public const string HideSplashScreenKey = "HideSplashScreen";
        public const string AutoMergeDirectlyKey = "AutoMergeDirectly";
        public const string LinkMergeWithWorkItemsKey = "LinkMergeWithWorkItems";
        public const string CheckInCommentTemplateKey = "CheckInCommentTemplate";
        public const string SelectedThemeKey = "SelectedTheme";
        public const string PerformNonModalMergeKey = "PerformNonModalMerge";

        public const string LocalWorkspaceBasePathKey = "LocalWorkspaceBasePath";

        public const string ProfileKey = "SettingProfiles_";

        public const string PrepareMergeDialogWindowViewSettingsKey = "PrepareMergeDialogWindowViewSettings";
        public const string ModalMergeDialogWindowSettingsKey = "ModalMergeDialogWindowSettings";
        public const string ConfigureColumnsWindowSettingsKey = "COnfigureColumnsWindowSettings";
        public const string IsCheckUtf8 = "IsCheckUtf8";


        public static string GetBranchCacheKeyForProjectCollectionUri(Uri projectCollection)
        {
            return "BranchCacheKey_" + projectCollection.ToString();
        }
    }
}
