using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ITfsUIInteractionProvider
    {
        void ShowWorkItem(int id);
        void ShowChangeset(int id);
        void TrackWorkItem(int id);
        void TrackChangeset(int id);
        void ResolveConflictsPerTF(string rootPath);
        bool ResolveConflictsInternally(ITfsTemporaryWorkspace workspace);
        bool ShowDifferencesPerTF(string rootPath, string sourcePath, string targetPath);
        string BrowseForTfsFolder(string startFrom);
    }
}
