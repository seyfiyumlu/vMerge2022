using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.Model.Implementation
{
    public class ProfileProvider : IProfileProvider
    {
        private IProfileSettings _activeProfile;

        public ProfileProvider()
        {
            Repository.Instance.TfsBridgeProvider.AfterCompleteBranchListLoaded +=
                (o, a) =>
                {
                    if (DefaultProfileChanged != null && GetDefaultProfile() != null)
                        DefaultProfileChanged(this, new DefaultProfileChangedEventArgs(GetDefaultProfile()));
                };
        }

        private SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>> _profiles;
        private SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>> Profiles
        {
            get
            {
                if (_profiles == null)
                {
                    _profiles = Repository.Instance.Settings.FetchSettings<SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>>>(Constants.Settings.ProfileKey);
                    if (_profiles == null)
                        _profiles = new SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>>();
                }

                return _profiles;
            }
        }

        public void ReloadFromSettings()
        {
            _profiles = Repository.Instance.Settings.FetchSettings<SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>>>(Constants.Settings.ProfileKey);
            if (_profiles == null)
                _profiles = new SerializableDictionary<string, SerializableDictionary<string, ProfileSettings>>();
            _activeProfile = null;
        }

        public void SetProfileDirty(IProfileSettings profileSettings)
        {
            if (profileSettings != null && profileSettings.Name == "__Default" && DefaultProfileChanged != null)
                DefaultProfileChanged(this, new DefaultProfileChangedEventArgs(profileSettings));
            if (profileSettings != null && profileSettings.Name == "__Default" && ProfilesChanged != null)
                ProfilesChanged(this, new DefaultProfileChangedEventArgs(profileSettings));

            Repository.Instance.Settings.SetSettings(Constants.Settings.ProfileKey, Profiles);
        }

        public IProfileSettings GetDefaultProfile(Uri teamProjectUri = null)
        {
            if (Repository.Instance.TfsBridgeProvider.ActiveTeamProject == null)
                return null;

            if (teamProjectUri==null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                result = new SerializableDictionary<string, ProfileSettings>();
                Profiles[teamProjectUri.ToString()] = result;
            }
            ProfileSettings settings = null;
            if (result.TryGetValue("__Default", out settings) == false)
            {
                var teamProject = Repository.Instance.TfsBridgeProvider.VersionControlServer.GetAllTeamProjects(false).Where(tp => tp.ArtifactUri.Equals(teamProjectUri)).FirstOrDefault();
                settings = new ProfileSettings(teamProjectUri.ToString(), teamProject.Name, "__Default", SetProfileDirty);
                result["__Default"] = settings;
            }
            return settings;
        }

        public IEnumerable<IProfileSettings> GetAllProfilesForProject(Uri teamProjectUri = null)
        {
            if (Repository.Instance.TfsBridgeProvider.ActiveTeamProject == null)
                return Enumerable.Empty<IProfileSettings>();

            if (teamProjectUri == null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                return Enumerable.Empty<IProfileSettings>();
            }

            return result.Values.Where(value => value.Name != "__Default");
        }
        
        public IEnumerable<IProfileSettings> GetAllProfiles()
        {
            return Profiles.Values.SelectMany(item => item.Values).Where(item => item.Name != "__Default");
        }

        public bool SaveProfileAs(Uri teamProjectUri, string profileName, bool overwrite)
        {
            if (teamProjectUri == null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                result = new SerializableDictionary<string, ProfileSettings>();
                Profiles[teamProjectUri.ToString()] = result;
            }

            if (result.ContainsKey(profileName) && !overwrite)
                return false;

            var teamProject = Repository.Instance.TfsBridgeProvider.VersionControlServer.GetAllTeamProjects(false).Where(tp => tp.ArtifactUri.Equals(teamProjectUri)).FirstOrDefault();
            var settings = new ProfileSettings(teamProjectUri.ToString(), teamProject.Name, profileName, SetProfileDirty);
            (GetDefaultProfile(teamProjectUri) as ProfileSettings).CopyTo(settings);
            result[profileName] = settings;

            if (ActiveProjectProfileListChanged != null && teamProjectUri == Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri)
                ActiveProjectProfileListChanged(this, EventArgs.Empty);
            if (ProfilesChanged != null)
                ProfilesChanged(this, EventArgs.Empty);
            _activeProfile = result[profileName];
            return true;
        }

        public bool DeleteProfile(IProfileSettings profile)
        {
            return DeleteProfile(new Uri(profile.TeamProject), profile.Name);
        }

        public bool DeleteProfile(Uri teamProjectUri, string profileName)
        {
            if (_activeProfile != null && _activeProfile.TeamProject==teamProjectUri.ToString() && _activeProfile.Name == profileName)
            {
                _activeProfile = null;
            }
            if (teamProjectUri == null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                result = new SerializableDictionary<string, ProfileSettings>();
                Profiles[teamProjectUri.ToString()] = result;
            }

            if (!result.ContainsKey(profileName))
                return false;

            result.Remove(profileName);

            if (ActiveProjectProfileListChanged != null && teamProjectUri == Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri)
                ActiveProjectProfileListChanged(this, EventArgs.Empty);
            if (ProfilesChanged != null)
                ProfilesChanged(this, EventArgs.Empty);
           
            SetProfileDirty(null);
            return true;
        }

        public bool LoadProfile(Uri teamProjectUri, string profileName)
        {
            if (teamProjectUri == null)
                teamProjectUri = Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri;

            SerializableDictionary<string, ProfileSettings> result = null;
            if (Profiles.TryGetValue(teamProjectUri.ToString(), out result) == false)
            {
                result = new SerializableDictionary<string, ProfileSettings>();
                Profiles[teamProjectUri.ToString()] = result;
            }

            if (!result.ContainsKey(profileName))
                return false;

            result[profileName].CopyTo(GetDefaultProfile(teamProjectUri));
            SetProfileDirty(GetDefaultProfile(teamProjectUri));
            _activeProfile = result[profileName];
            return true;
        }

        public bool GetActiveProfile(out IProfileSettings mostRecentSettings, out bool alreadyModified)
        {
            var defaultProfile = GetDefaultProfile();
            mostRecentSettings = _activeProfile;
            alreadyModified = false;
            if (defaultProfile==null)
                return false;

            if (mostRecentSettings==null)
            {
                alreadyModified = true;
            }
            else
            {
                alreadyModified = !mostRecentSettings.Equals(defaultProfile);
            }
            return true;
        }

        public event EventHandler<DefaultProfileChangedEventArgs> DefaultProfileChanged;

        public event EventHandler ProfilesChanged;

        public event EventHandler ActiveProjectProfileListChanged;

    }
}
