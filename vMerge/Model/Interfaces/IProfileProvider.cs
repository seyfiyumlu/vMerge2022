using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public class DefaultProfileChangedEventArgs : EventArgs
    {
        public DefaultProfileChangedEventArgs(IProfileSettings settings)
        {
            Settings = settings;
        }

        public IProfileSettings Settings
        {
            get;
            private set;
        }
    }

    public interface IProfileProvider
    {
        IProfileSettings GetDefaultProfile(Uri teamProjectUri = null);
        IEnumerable<IProfileSettings> GetAllProfilesForProject(Uri teamProjectUri = null);
        IEnumerable<IProfileSettings> GetAllProfiles();
        bool SaveProfileAs(Uri teamProjectUri, string profileName, bool overwrite);
        bool DeleteProfile(Uri teamProjectUri, string profileName);
        bool DeleteProfile(IProfileSettings profile);
        bool LoadProfile(Uri teamProjectUri, string profileName);
        bool GetActiveProfile(out IProfileSettings mostRecentSettings, out bool alreadyModified);

        event EventHandler<DefaultProfileChangedEventArgs> DefaultProfileChanged;
        event EventHandler ProfilesChanged;
        event EventHandler ActiveProjectProfileListChanged;
        void SetProfileDirty(IProfileSettings profileSettings);
    }
}
