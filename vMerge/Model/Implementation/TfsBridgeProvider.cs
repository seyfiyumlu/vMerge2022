using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.IO;
using alexbegh.Utility.Helpers.InOrderSetter;
using alexbegh.Utility.Helpers.Logging;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using System.Security.Principal;
using System.Diagnostics;

namespace alexbegh.vMerge.Model.Implementation
{
    class TfsBridgeProvider : ITfsBridgeProvider
    {
        #region Properties
        /// <summary>
        /// Private lock target
        /// </summary>
        private object _locker = new object();

        /// <summary>
        /// TfsTeamProjectCollection
        /// </summary>
        private volatile TfsTeamProjectCollection _tfsTeamProjectCollection;
        public TfsTeamProjectCollection TfsTeamProjectCollection
        {
            get
            {
                var tpc = _tfsTeamProjectCollection;
                if (tpc != null)
                    return tpc;
                var uri = Repository.Instance.TfsConnectionInfo.Uri;
                if (uri == null)
                    return null;
                lock (_locker)
                {
                    try
                    {
                        if (_tfsTeamProjectCollection != null)
                            return _tfsTeamProjectCollection;
                        _tfsTeamProjectCollection = new TfsTeamProjectCollection(uri);
                        _tfsTeamProjectCollection.EnsureAuthenticated();
                    }
                    catch (Exception)
                    {
                        _tfsTeamProjectCollection = null;
                    }
                    return _tfsTeamProjectCollection;
                }
            }
            set
            {
                lock (_locker)
                {
                    _tfsTeamProjectCollection = value;
                    _tfsWorkItemStore = null;
                }
                RaisePropertyChanged("TfsTeamProjectCollection");
            }
        }

        /// <summary>
        /// WorkItemStore
        /// </summary>
        private volatile WorkItemStore _tfsWorkItemStore;
        public WorkItemStore TfsWorkItemStore
        {
            get
            {
                var tpc = _tfsWorkItemStore;
                if (tpc != null)
                    return tpc;
                var pc = TfsTeamProjectCollection;
                if (pc == null)
                    return null;
                lock (_locker)
                {
                    try
                    {
                        if (_tfsWorkItemStore != null)
                            return _tfsWorkItemStore;
                        _tfsWorkItemStore = pc.GetService<WorkItemStore>();
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Log(ex);
                        _tfsWorkItemStore = null;
                    }
                    return _tfsWorkItemStore;
                }
            }
            set
            {
                lock (_locker)
                {
                    _tfsWorkItemStore = value;
                }
                RaisePropertyChanged("TfsWorkItemStore");
            }
        }

        /// <summary>
        /// VersionControlServer
        /// </summary>
        private volatile VersionControlServer _versionControlServer;
        public VersionControlServer VersionControlServer
        {
            get
            {
                var vcs = _versionControlServer;
                if (vcs != null)
                    return vcs;
                var pc = TfsTeamProjectCollection;
                lock (_locker)
                {
                    if (_versionControlServer != null)
                        return _versionControlServer;
                    if (pc == null)
                        return null;

                    _versionControlServer = pc.GetService<VersionControlServer>();
                    Task.Run(
                        () =>
                        {
                            var workspaces = _versionControlServer.QueryWorkspaces(null, null, Environment.MachineName);
                            foreach (var workspace in workspaces)
                            {
                                if (workspace.Name.StartsWith("vMerge"))
                                {
                                    string name = workspace.Name;
                                    int dot = name.IndexOf('.');
                                    if (dot >= 0)
                                    {
                                        int pid = 0;
                                        if (int.TryParse(name.Substring(6, dot - 6), out pid))
                                        {
                                            var existing = Process.GetProcesses().FirstOrDefault(process => process.Id == pid);
                                            if (existing == null)
                                            {
                                                DeleteTemporaryWorkspace(workspace, null);
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    return _versionControlServer;
                }
            }
            private set
            {
                lock (_locker)
                {
                    _versionControlServer = value;
                }
                RaisePropertyChanged("VersionControlServer");
            }
        }

        /// <summary>
        /// The currently selected team project
        /// </summary>
        private volatile TeamProject _activeTeamProject;
        public TeamProject ActiveTeamProject
        {
            get
            {
                return _activeTeamProject;
            }
            set
            {
                if (_activeTeamProject != value)
                {
                    if (_activeTeamProject == null || value == null || _activeTeamProject.ArtifactUri != value.ArtifactUri)
                    {
                        _activeTeamProject = value;
                        lock (_locker)
                        {
                            CompleteBranchList = null;
                            RootQuery = null;
                            if (_activeTeamProject != null)
                            {
                                Repository.Instance.BackgroundTaskManager.Start(
                                    Constants.Tasks.LoadCompleteBranchListTaskKey, null, (task) => LoadCompleteBranchList());
                                Repository.Instance.BackgroundTaskManager.Start(
                                    Constants.Tasks.LoadRootQueryTaskKey, null, (task) => LoadRootQuery());
                            }
                        }
                        if (_activeProjectSelected != null)
                        {
                            Repository.Instance.BackgroundTaskManager.Send(
                                () =>
                                {
                                    var deleg = _activeProjectSelected;
                                    if (deleg != null)
                                        deleg(this, null);
                                    return true;
                                }
                            );
                        }
                    }
                    RaisePropertyChanged("ActiveTeamProject");
                }
            }
        }

        /// <summary>
        /// The complete list of available branches in the current team project
        /// </summary>
        private SetInOrder<ObservableCollection<ITfsBranch>> _completeBranchList = new SetInOrder<ObservableCollection<ITfsBranch>>();
        public ObservableCollection<ITfsBranch> CompleteBranchList
        {
            get
            {
                // Undefined if the active team project is not set
                if (ActiveTeamProject == null)
                    return null;

                return _completeBranchList.Item;
            }
            private set
            {
                lock (_locker)
                {
                    _completeBranchList.SetItem(value, _completeBranchList.GetSetNo());
                }
                RaisePropertyChanged("CompleteBranchList");
            }
        }

        private SetInOrder<ITfsQueryFolder> _rootQuery = new SetInOrder<ITfsQueryFolder>();
        public ITfsQueryFolder RootQuery
        {
            get
            {
                if (_activeTeamProject == null)
                    return null;
                var rq = _rootQuery.Item;
                return rq;
            }
            private set
            {
                lock (_locker)
                {
                    _rootQuery.SetItem(value, _rootQuery.GetSetNo());
                }
                RaisePropertyChanged("RootQuery");
            }
        }

        private Dictionary<string, WeakReference> _cachedSubBranches;
        internal Dictionary<string, WeakReference> CachedSubBranches
        {
            get
            {
                lock (_locker)
                {
                    if (_cachedSubBranches == null)
                        _cachedSubBranches = new Dictionary<string, WeakReference>();
                    return _cachedSubBranches;
                }
            }
        }

        private event EventHandler _completeBranchListLoaded;
        public event EventHandler CompleteBranchListLoaded
        {
            add { _completeBranchListLoaded += value; }
            remove { _completeBranchListLoaded -= value; }
        }

        private event EventHandler _afterCompleteBranchListLoaded;
        public event EventHandler AfterCompleteBranchListLoaded
        {
            add { _afterCompleteBranchListLoaded += value; }
            remove { _afterCompleteBranchListLoaded -= value; }
        }

        private event EventHandler _activeProjectSelected;
        public event EventHandler ActiveProjectSelected
        {
            add { _activeProjectSelected += value; }
            remove { _activeProjectSelected -= value; }
        }

        private event EventHandler _rootQueryLoaded;
        public event EventHandler RootQueryLoaded
        {
            add { _rootQueryLoaded += value; }
            remove { _rootQueryLoaded -= value; }
        }

        private event EventHandler _completeBranchListLoading;
        public event EventHandler CompleteBranchListLoading
        {
            add { _completeBranchListLoading += value; }
            remove { _completeBranchListLoading -= value; }
        }

        private event EventHandler _rootQueryLoading;
        public event EventHandler RootQueryLoading
        {
            add { _rootQueryLoading += value; }
            remove { _rootQueryLoading -= value; }
        }
        #endregion

        #region Public Operations
        #region Is / Get Operations
        /// <summary>
        /// The list of merge targets
        /// </summary>
        public IEnumerable<ITfsBranch> GetPossibleMergeTargetBranches(ITfsBranch sourceBranch, BranchSearchOptions searchOptions = BranchSearchOptions.Both)
        {
            if (sourceBranch.IsSubBranch)
                return sourceBranch.ChildBranches.AsEnumerable();

            var result = new ObservableCollection<ITfsBranch>();

            var listOfForeignBranchesReferencingSourceBranch
                = (searchOptions == BranchSearchOptions.Both || searchOptions == BranchSearchOptions.Upwards)
                    ? CompleteBranchList
                        .Where(item => item.ChildBranchNames.Any(cb => cb == sourceBranch.Name))
                    : Enumerable.Empty<ITfsBranch>();

            var listOfSourceBranchChildren
                = (searchOptions == BranchSearchOptions.Both || searchOptions == BranchSearchOptions.Downwards)
                    ? sourceBranch.ChildBranchNames
                        .Select(cb => CompleteBranchList.Where(item => item.Name == cb).FirstOrDefault())
                    : Enumerable.Empty<ITfsBranch>();

            return listOfForeignBranchesReferencingSourceBranch
                    .Union(listOfSourceBranchChildren)
                    .Where(item => item != null)
                    .OrderBy(item => item.Name);
        }

        /// <summary>
        /// Loads the root query
        /// </summary>
        public void LoadRootQuery()
        {
            long no = -1;
            ITfsQueryFolder rq = null;

            RaiseRootQueryLoading();
            no = _rootQuery.GetSetNo();
            rq = GetAllQueries();

            if (_rootQuery.SetItem(rq, no))
            {
                RaiseRootQueryLoaded();
            }
        }

        public void LoadCompleteBranchList()
        {
            long no1 = -1, no2 = -1;
            BranchObject[] allBranches;

            var tfsTeamProjectCollection = TfsTeamProjectCollection;

            try
            {
                SimpleLogger.Log(SimpleLogLevel.Info, "Loading CBL, {0},{1},{2}", _completeBranchList != null, tfsTeamProjectCollection != null, tfsTeamProjectCollection != null ? (tfsTeamProjectCollection.Uri != null).ToString() : "--");
                RaiseCompleteBranchListLoading();
                no1 = _completeBranchList.GetSetNo();
                var cachedList = Repository.Instance.Settings.FetchSettings<List<TfsBranch>>(Constants.Settings.GetBranchCacheKeyForProjectCollectionUri(tfsTeamProjectCollection.Uri));
                if (cachedList != null)
                {
                    foreach (var item in cachedList)
                        item.Vcs = VersionControlServer;

                    var cachedcbl = new ObservableCollection<ITfsBranch>(cachedList.Cast<ITfsBranch>());
                    SimpleLogger.Log(SimpleLogLevel.Info, "Trying to set CBL");
                    if (_completeBranchList.SetItem(cachedcbl, no1))
                    {
                        SimpleLogger.Log(SimpleLogLevel.Info, "Set CBL {0}", cachedcbl != null);
                        RaiseCompleteBranchListLoaded();
                    }
                }
                else
                {
                    allBranches = VersionControlServer.QueryRootBranchObjects(RecursionType.Full);
                    var tcbl = allBranches
                                .Where(item => !item.Properties.RootItem.IsDeleted)
                                .Select(item => new TfsBranch(_versionControlServer, item))
                                .OrderBy(item => item.Name).ToList();
                    Repository.Instance.Settings.SetSettings(Constants.Settings.GetBranchCacheKeyForProjectCollectionUri(tfsTeamProjectCollection.Uri), tcbl.Cast<TfsBranch>().ToList());
                    SimpleLogger.Log(SimpleLogLevel.Info, "Trying to set RBL");
                    if (_completeBranchList.SetItem(new ObservableCollection<ITfsBranch>(tcbl), no1))
                    {
                        SimpleLogger.Log(SimpleLogLevel.Info, "Set RBL {0}", tcbl != null);
                        RaiseCompleteBranchListLoaded();
                    }
                    return;
                }

                SimpleLogger.Log(SimpleLogLevel.Info, "Updating CBL");
                no2 = _completeBranchList.GetSetNo();
                allBranches = VersionControlServer.QueryRootBranchObjects(RecursionType.Full);
                SimpleLogger.Log(SimpleLogLevel.Info, "Loaded CBL");
                ObservableCollection<ITfsBranch> cbl;

                if (CompleteBranchList != null)
                    cbl = new ObservableCollection<ITfsBranch>(CompleteBranchList);
                else
                    cbl = new ObservableCollection<ITfsBranch>();

                lock (_locker)
                {
                    var tcbl = allBranches
                                .Where(item => !item.Properties.RootItem.IsDeleted)
                                .Select(item => new TfsBranch(_versionControlServer, item))
                                .OrderBy(item => item.Name).ToList();
                    foreach (var branch in cbl)
                    {
                        var found = tcbl.FirstOrDefault(b => b.Name == branch.Name);
                        if (found != null)
                        {
                            ((TfsBranch)branch).BranchObject = found.BranchObject;
                            ((TfsBranch)branch).ChildBranchNames = found.ChildBranchNames;
                        }
                    }

                    var missing = tcbl.Except(cbl).ToArray();
                    foreach (var branch in missing)
                    {
                        cbl.Add((TfsBranch)branch);
                    }

                    var superfluous = cbl.Except(tcbl).ToArray();
                    foreach (var branch in superfluous)
                    {
                        cbl.Remove(branch);
                    }
                }
                SimpleLogger.Log(SimpleLogLevel.Info, "Before setting CBL");
                RaiseCompleteBranchListLoading();
                if (_completeBranchList.SetItem(cbl, no2))
                {
                    SimpleLogger.Log(SimpleLogLevel.Info, "Setting CBL");
                    Repository.Instance.Settings.SetSettings(Constants.Settings.GetBranchCacheKeyForProjectCollectionUri(tfsTeamProjectCollection.Uri), cbl.Cast<TfsBranch>().ToList());
                    RaiseCompleteBranchListLoaded();
                    SimpleLogger.Log(SimpleLogLevel.Info, "Set CBL");
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, false);
            }
        }

        /// <summary>
        /// Returns a branch by specifying the fully qualified TFS source control path
        /// </summary>
        /// <param name="sourceBranch">The source branches path</param>
        /// <returns>The TFS branch, null if no branch exists</returns>
        public ITfsBranch GetBranchByNameOrNull(string sourceBranch)
        {
            return GetSpecificSubBranch(sourceBranch);
        }


        /// <summary>
        /// Tries to find the matching item in the target branch.
        /// </summary>
        /// <param name="targetBranch">The target branch</param>
        /// <param name="serverItem">The item</param>
        /// <returns>The serverItem path in the target branch if found, null otherwise</returns>
        public string GetPathInTargetBranch(ITfsBranch targetBranch, string serverItem)
        {
            var res = VersionControlServer.GetBranchHistory(new ItemSpec[] { new ItemSpec(serverItem, RecursionType.None) }, LatestVersionSpec.Latest);
            var targetTo = res[0].SelectMany(item => item.Children.Cast<BranchHistoryTreeItem>()).Where(item => item.Relative.BranchToItem.ServerItem.StartsWith(targetBranch.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            var targetFrom = res[0].SelectMany(item => item.Children.Cast<BranchHistoryTreeItem>()).Where(item => item.Relative.BranchFromItem.ServerItem.StartsWith(targetBranch.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            return targetTo != null
                ? targetTo.Relative.BranchToItem.ServerItem
                : (targetFrom != null
                   ? targetFrom.Relative.BranchFromItem.ServerItem
                   : null);

        }

        /// <summary>
        /// Acquires the list of possible merge candidates from the given source to destination branch
        /// </summary>
        /// <param name="source">The source branch</param>
        /// <param name="destination">The target branch</param>
        /// <returns>List of changesets</returns>
        public IEnumerable<ITfsChangeset> GetMergeCandidatesForBranchToBranch(ITfsBranch source, ITfsBranch destination, string sourcePathFilter = null)
        {
            string sourcePath = null, targetPath = null;

            if (sourcePathFilter != null)
                sourcePath = sourcePathFilter;
            else
                sourcePath = source.ServerPath;

            if (sourcePath != source.ServerPath)
            {
                targetPath = GetPathInTargetBranch(destination, sourcePath);
                if (targetPath == null)
                {
                    sourcePath = source.ServerPath;
                    targetPath = destination.ServerPath;
                }
            }
            else
                targetPath = destination.ServerPath;

            return
                VersionControlServer.GetMergeCandidates(sourcePath, targetPath, RecursionType.Full)
                .Select(item => new TfsChangeset(item.Changeset));
        }

        /// <summary>
        /// Acquires the full list of changesets from the given branch
        /// </summary>
        /// <param name="branch">The branch</param>
        /// <returns>List of changesets</returns>
        public IEnumerable<ITfsChangeset> GetAllChangesetsForBranch(ITfsBranch branch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the changesets which have resulted in the given changeset due
        /// to a merge operation.
        /// </summary>
        /// <param name="changeset">The changeset.</param>
        /// <param name="versionControlServer">The version control server.</param>
        /// <returns>
        /// A list of all changesets that have resulted into the given changeset.
        /// </returns>
        public IEnumerable<ITfsChangeset> GetMergeSourceChangesets(ITfsChangeset changeset)
        {
            VersionControlServer versionControlServer = Repository.Instance.TfsBridgeProvider.VersionControlServer;

            // remember the already covered changeset id's
            Dictionary<int, bool> alreadyCoveredChangesets = new Dictionary<int, bool>();

            // initialize list of parent changesets
            List<Changeset> parentChangesets = new List<Changeset>();

            // go through each change inside the changeset
            foreach (Change change in changeset.Changeset.Changes)
            {
                // query for the items' history
                var queryResults = versionControlServer.QueryMergesExtended(
                                        new ItemSpec(change.Item.ServerItem, RecursionType.Full),
                                        new ChangesetVersionSpec(changeset.Changeset.ChangesetId),
                                        null,
                                        null);

                // go through each changeset in the history
                foreach (var result in queryResults)
                {
                    // only if the target-change is the given changeset, we have a hit
                    if (result.TargetChangeset.ChangesetId == changeset.Changeset.ChangesetId)
                    {
                        // if that hit has already been processed elsewhere, then just skip it
                        if (!alreadyCoveredChangesets.ContainsKey(result.SourceChangeset.ChangesetId))
                        {
                            // otherwise add it
                            alreadyCoveredChangesets.Add(result.SourceChangeset.ChangesetId, true);
                            yield return new TfsChangeset(versionControlServer.GetChangeset(result.SourceChangeset.ChangesetId));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a specific work item by its id
        /// </summary>
        /// <param name="id">The id to query</param>
        /// <returns>The work item, null otherwise</returns>
        public ITfsWorkItem GetWorkItemById(int id)
        {
            return new TfsWorkItem(TfsWorkItemStore.GetWorkItem(id));
        }

        /// <summary>
        /// Retrieves a specific changeset by its id
        /// </summary>
        /// <param name="id">The id to query</param>
        /// <returns>The changeset, null otherwise</returns>
        public ITfsChangeset GetChangesetById(int id)
        {
            return new TfsChangeset(VersionControlServer.GetChangeset(id));
        }

        /// <summary>
        /// Returns the list of related changesets for a given work item
        /// </summary>
        /// <param name="workItem">The work item to query</param>
        /// <returns>The list of changesets</returns>
        public IEnumerable<ITfsChangeset> GetRelatedChangesetsForWorkItem(ITfsWorkItem workItem)
        {
            var changesetLinkType = TfsWorkItemStore.RegisteredLinkTypes[ArtifactLinkIds.Changeset];
            var artifactProvider = VersionControlServer.ArtifactProvider;
            return
                workItem.WorkItem.Links
                    .OfType<ExternalLink>()
                    .Where(type => type.ArtifactLinkType.Equals(changesetLinkType))
                    .Select(link => new TfsChangeset(artifactProvider.GetChangeset(new Uri(link.LinkedArtifactUri))));
        }

        /// <summary>
        /// Gets the no. of changesets for a given work item (fast than fetching all)
        /// </summary>
        /// <param name="workItem">The work item to query</param>
        /// <returns>The list of changesets</returns>
        public int GetRelatedChangesetsForWorkItemCount(ITfsWorkItem workItem)
        {
            var changesetLinkType = TfsWorkItemStore.RegisteredLinkTypes[ArtifactLinkIds.Changeset];
            var artifactProvider = VersionControlServer.ArtifactProvider;
            return
                workItem.WorkItem.Links.OfType<ExternalLink>().Where(type => type.ArtifactLinkType.Equals(changesetLinkType)).Count();
        }

        /// <summary>
        /// Checks if the full list of branches has already been loaded
        /// </summary>
        /// <returns>true if loaded</returns>
        public bool IsCompleteBranchListLoaded()
        {
            return _completeBranchList.Item != null;
        }

        /// <summary>
        /// Checks if the full list of queries for the active team project have already been loaded
        /// </summary>
        /// <returns>true if loaded</returns>
        public bool IsRootQueryLoaded()
        {
            return _rootQuery != null;
        }

        /// <summary>
        /// Reinitializes the state
        /// </summary>
        public void Clear()
        {
            RootQuery = null;
            CompleteBranchList = null;
            TfsTeamProjectCollection = null;
            VersionControlServer = null;
            TfsWorkItemStore = null;
            Repository.Instance.BackgroundTaskManager.Start(Constants.Tasks.RefreshTaskKey, null, (task) =>
                {
                    var rootQuery = RootQuery;
                    task.Cancelled.Token.ThrowIfCancellationRequested();
                    var completeBranchList = CompleteBranchList;
                    task.Cancelled.Token.ThrowIfCancellationRequested();
                    var tfsTpc = TfsTeamProjectCollection;
                    task.Cancelled.Token.ThrowIfCancellationRequested();
                    var vcs = VersionControlServer;
                    task.Cancelled.Token.ThrowIfCancellationRequested();
                    var tfsWis = TfsWorkItemStore;
                });
        }

        public void Refresh()
        {
            Repository.Instance.BackgroundTaskManager.CancelAll();
            RootQuery = null;
            CompleteBranchList = null;
            TfsTeamProjectCollection = null;
            VersionControlServer = null;
            TfsWorkItemStore = null;
            var atp = ActiveTeamProject != null ? ActiveTeamProject.ArtifactUri : null;
            ActiveTeamProject = null;
            Repository.Instance.BackgroundTaskManager.Start(Constants.Tasks.RefreshTaskKey, null, (task) =>
                {
                    if (atp != null && VersionControlServer != null)
                        ActiveTeamProject = VersionControlServer.GetAllTeamProjects(true).Where(tp => tp.ArtifactUri == atp).First();
                });
        }
        public TeamFoundationIdentity[] GetUsers()
        {
            var ims = TfsTeamProjectCollection.GetService<IIdentityManagementService>();

            // Get expanded membership of the Valid Users group, which is all identities in this host             
            var group = ims.ReadIdentity(GroupWellKnownDescriptors.EveryoneGroup, MembershipQuery.Expanded, ReadIdentityOptions.None);
            var resultIdentities = new HashSet<TeamFoundationIdentity>(); ;

            // If total membership exceeds batch size limit for Read, break it up
            int batchSizeLimit = 100000;
            var descriptors = group.Members;

            if (descriptors.Length > batchSizeLimit)
            {
                TeamFoundationIdentity[] identities;
                int batchNum = 0;
                int remainder = descriptors.Length;
                IdentityDescriptor[] batchDescriptors = new IdentityDescriptor[batchSizeLimit];

                while (remainder > 0)
                {
                    int startAt = batchNum * batchSizeLimit;
                    int length = batchSizeLimit;
                    if (length > remainder)
                    {
                        length = remainder;
                        batchDescriptors = new IdentityDescriptor[length];
                    }

                    Array.Copy(descriptors, startAt, batchDescriptors, 0, length);
                    identities = ims.ReadIdentities(batchDescriptors, MembershipQuery.Direct, ReadIdentityOptions.None);
                    resultIdentities.UnionWith(identities);
                    remainder -= length;
                }
            }
            else
            {
                resultIdentities.UnionWith(ims.ReadIdentities(descriptors, MembershipQuery.Direct, ReadIdentityOptions.None));
            }
            var validWebUsers = resultIdentities.Where(identity => identity.Descriptor.Identifier.EndsWith("@live.com", StringComparison.InvariantCultureIgnoreCase) && identity.IsActive == true && identity.IsContainer == false).ToArray();
            var validLocalUsers = resultIdentities.Where(identity => identity.Descriptor.IdentityType == "System.Security.Principal.WindowsIdentity" && identity.IsActive == true && identity.IsContainer == false).ToArray();
            var users = validWebUsers.Concat(validLocalUsers).Where(identity => System.Globalization.CultureInfo.InvariantCulture.CompareInfo.IndexOf(identity.DisplayName, "build", System.Globalization.CompareOptions.IgnoreCase) < 0);

            return users
                    .ToArray();
        }
        #endregion

        #region Create Operations
        private Dictionary<ITfsBranch, Dictionary<ITfsBranch, ITfsTemporaryWorkspace>> _temporaryWorkspaces;

        /// <summary>
        /// Creates a temporary workspace
        /// </summary>
        /// <returns>Temporary workspace</returns>
        public ITfsTemporaryWorkspace GetTemporaryWorkspace(ITfsBranch source, ITfsBranch target)
        {
            lock (_locker)
            {
                if (_temporaryWorkspaces == null)
                {
                    _temporaryWorkspaces = new Dictionary<ITfsBranch, Dictionary<ITfsBranch, ITfsTemporaryWorkspace>>();
                }
                if (!_temporaryWorkspaces.ContainsKey(source))
                {
                    _temporaryWorkspaces[source] = new Dictionary<ITfsBranch, ITfsTemporaryWorkspace>();
                }
                if (_temporaryWorkspaces[source].ContainsKey(target))
                {
                    try
                    {
                        var existingWs = _temporaryWorkspaces[source][target];
                        var ws = VersionControlServer.QueryWorkspaces(existingWs.TfsWorkspace.Name, TfsTeamProjectCollection.ConfigurationServer.AuthorizedIdentity.UniqueName, Environment.MachineName);
                        if (ws.Any())
                            return existingWs;
                    }
                    catch (Exception)
                    {
                    }
                }

                string randomWorkspaceName;
                string localMapping;
                bool found = false;
                do
                {
                    randomWorkspaceName = "vMerge" + Process.GetCurrentProcess().Id.ToString() + "." + Guid.NewGuid().ToString().Replace("-", "").Replace("{", "").Replace("}", "").Substring(0, 6);
                    var ws = VersionControlServer.QueryWorkspaces(randomWorkspaceName, TfsTeamProjectCollection.ConfigurationServer.AuthorizedIdentity.UniqueName, Environment.MachineName);
                    found = ws.Any();

                    localMapping = Path.Combine(GetBasePath(), randomWorkspaceName);
                    if (Directory.Exists(localMapping))
                        found = false;
                } while (found);

                var tfsWorkspace = VersionControlServer.CreateWorkspace(randomWorkspaceName);
                tfsWorkspace.Map("$/", localMapping);
                var workspace = new TfsTemporaryWorkspace(this, tfsWorkspace, source, localMapping, target.Name);
                _temporaryWorkspaces[source][target] = workspace;
                return workspace;
            }
        }

        public void DisposeTemporaryWorkspaces()
        {
            lock (_locker)
            {
                foreach (var workspaces in _temporaryWorkspaces.Values)
                {
                    foreach (var workspace in workspaces.Values)
                    {
                        workspace.Dispose();
                    }
                }
                _temporaryWorkspaces = new Dictionary<ITfsBranch, Dictionary<ITfsBranch, ITfsTemporaryWorkspace>>();
            }
        }
        #endregion

        #region Internal Operations
        /// <summary>
        /// Deletes an existing work space
        /// </summary>
        /// <param name="workspace">The workspace to delete</param>
        /// <param name="mappedFolder">The mapped folder on local disk</param>
        internal void DeleteTemporaryWorkspace(Workspace workspace, string mappedFolder)
        {
            if (mappedFolder == null)
            {
                mappedFolder = Path.Combine(GetBasePath(), workspace.Name);
            }

            workspace.Delete();
            try
            {
                Directory.Delete(mappedFolder, true);
            }
            catch (Exception)
            {
            }
        }
        #endregion
        #endregion

        #region Private Operations
        /// <summary>
        /// Creates an internal representation of the given projects query hierarchy
        /// </summary>
        /// <param name="project">The project</param>
        /// <param name="q">The current queryfolder</param>
        /// <param name="qi">The internal wrapper for the current queryfolder</param>
        /// <param name="level">The recursion depth</param>
        private void ProcessQueryHierarchy(Project project, QueryFolder q, TfsQueryFolder qi, int level)
        {
            foreach (QueryItem item in q)
            {
                if (item is QueryFolder)
                {
                    TfsQueryFolder sub = new TfsQueryFolder()
                    {
                        Title = item.Name,
                        Children = new List<ITfsQueryItem>(),
                        Parent = qi,
                        Level = level
                    };
                    qi.Children.Add(sub);
                    ProcessQueryHierarchy(project, item as QueryFolder, sub, level + 1);
                }
                else
                {
                    var wis = TfsTeamProjectCollection.GetService<WorkItemStore>();
                    TfsQuery sub = new TfsQuery(wis, project)
                    {
                        Title = item.Name,
                        Parent = qi,
                        QueryDefinition = (item as QueryDefinition),
                        Level = level
                    };
                    qi.Children.Add(sub);
                }
            }
        }

        /// <summary>
        /// Entry point method for fetching all queries for a given project (the active one)
        /// </summary>
        /// <returns>The root query folder for the active project</returns>
        private ITfsQueryFolder GetAllQueries()
        {
            try
            {
                var wis = TfsTeamProjectCollection.GetService<WorkItemStore>();
                var project = wis.Projects[ActiveTeamProject.Name];
                var queryHierarchy = project.QueryHierarchy;

                var result = new TfsQueryFolder()
                {
                    Title = queryHierarchy.Name,
                    Children = new List<ITfsQueryItem>(),
                    Parent = null,
                    Level = 0
                };
                ProcessQueryHierarchy(project, queryHierarchy, result, 1);
                return result;
            }
            catch (NullReferenceException)
            {
                // This can happen due to a parallel refresh (ActiveTeamProject switches to null)
                return null;
            }
        }

        private string GetBasePath()
        {
            string basePath = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.LocalWorkspaceBasePathKey)
                ?? Path.GetTempPath();

            return basePath;
        }

        private ITfsBranch GetSpecificSubBranch(string serverPath)
        {
            ITfsBranch subBranch = null;
            WeakReference cachedSubBranchWeak;
            lock (_locker)
            {
                if (CachedSubBranches.TryGetValue(serverPath, out cachedSubBranchWeak))
                {
                    // Intentionally null?
                    if (cachedSubBranchWeak==null)
                        return null;
                    subBranch = cachedSubBranchWeak.Target as ITfsBranch;
                }
            }
            if (subBranch != null)
                return subBranch;

            subBranch  = CompleteBranchList.FirstOrDefault(cb => cb.ServerPath == serverPath);
            if (subBranch == null)
            {
                subBranch = new TfsBranch(VersionControlServer, serverPath, true);
                if (subBranch.ChildBranches.Count==0)
                    subBranch = null;
            }
            
            cachedSubBranchWeak = subBranch != null ? new WeakReference(subBranch) : null;
            lock (_locker)
            {
                if (false == CachedSubBranches.ContainsKey(serverPath))
                    CachedSubBranches[serverPath] = cachedSubBranchWeak;
            }
            return subBranch;
        }

        private void RaiseCompleteBranchListLoading()
        {
            if (_completeBranchListLoading != null)
            {
                Repository.Instance.BackgroundTaskManager.Send(
                    () =>
                    {
                        _completeBranchListLoading(this, new EventArgs());

                        return true;
                    });
            }
        }

        private void RaiseCompleteBranchListLoaded()
        {
            if (_completeBranchListLoaded != null)
            {
                Repository.Instance.BackgroundTaskManager.Send(
                    () =>
                    {
                        _completeBranchListLoaded(this, new EventArgs());
                        if (_afterCompleteBranchListLoaded != null)
                            _afterCompleteBranchListLoaded(this, new EventArgs());

                        return true;
                    });
            }
        }

        private void RaiseRootQueryLoaded()
        {
            if (_rootQueryLoaded != null)
            {
                Repository.Instance.BackgroundTaskManager.Send(
                    () =>
                    {
                        _rootQueryLoaded(this, new EventArgs());

                        return true;
                    });
            }
        }

        private void RaiseRootQueryLoading()
        {
            if (_rootQueryLoading != null)
            {
                Repository.Instance.BackgroundTaskManager.Send(
                    () =>
                    {
                        _rootQueryLoading(this, new EventArgs());
                        return true;
                    });
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
