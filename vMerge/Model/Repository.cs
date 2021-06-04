using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.Managers.Background;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.Model.Implementation;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Threading;

namespace alexbegh.vMerge.Model
{
    class Repository
    {
        private static object _staticLocker = new object();

        private static int _uiThreadId = -1;

        public static void Initialize(ITfsConnectionInfoProvider tfsConnectionInfoProvider, ITfsUIInteractionProvider tfsUIInteractionProvider, IVMergeUIProvider vMergeUIProvider)
        {
            if (_instance != null)
                throw new InvalidOperationException("Already initialized!");

            _instance = new Repository(tfsConnectionInfoProvider, tfsUIInteractionProvider, vMergeUIProvider);
        }

        private Repository(ITfsConnectionInfoProvider tfsConnectionInfoProvider, ITfsUIInteractionProvider tfsUIInteractionProvider, IVMergeUIProvider vMergeUIProvider)
        {
            try
            {
                InitBackgroundTaskManager();

                _tfsConnectionInfo = tfsConnectionInfoProvider;
                _tfsUIInteractionProvider = tfsUIInteractionProvider;
                _vMergeUIProvider = vMergeUIProvider;
                SynchronizationContext context = SynchronizationContext.Current;
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
            }
        }

        private static volatile Repository _instance;
        public static Repository Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("Call Initialize first");
                return _instance;
            }
        }
        public static void SetUIThread()
        {
            _uiThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static void ThrowIfNotUIThread(string message = null)
        {
            if (Thread.CurrentThread.ManagedThreadId != _uiThreadId)
            {
                if (_uiThreadId == -1)
                    throw new InvalidOperationException("Repository is not correctly initialized");

                if (message != null)
                    throw new InvalidOperationException(message);
                else
                    throw new InvalidOperationException();
            }
        }

        private volatile ITfsConnectionInfoProvider _tfsConnectionInfo;
        public ITfsConnectionInfoProvider TfsConnectionInfo
        {
            get
            {
                return _tfsConnectionInfo;
            }
        }

        private volatile ITfsUIInteractionProvider _tfsUIInteractionProvider;
        public ITfsUIInteractionProvider TfsUIInteractionProvider
        {
            get
            {
                return _tfsUIInteractionProvider;
            }
        }

        private IVMergeUIProvider _vMergeUIProvider;
        public IVMergeUIProvider VMergeUIProvider
        {
            get
            {
                return _vMergeUIProvider;
            }
        }

        private volatile ITfsBridgeProvider _tfsBridgeProvider;
        public ITfsBridgeProvider TfsBridgeProvider
        {
            get
            {
                var tfsBridge = _tfsBridgeProvider;
                if (tfsBridge != null)
                    return tfsBridge;
                lock (_staticLocker)
                {
                    if (_tfsBridgeProvider != null)
                        return _tfsBridgeProvider;
                    _tfsBridgeProvider = new TfsBridgeProvider();
                    return _tfsBridgeProvider;
                }
            }
            set
            {
                lock (_staticLocker)
                {
                    _tfsBridgeProvider = value;
                }
            }
        }

        private volatile ISettings _settings;
        public ISettings Settings
        {
            get
            {
                var settings = _settings;
                if (settings != null)
                    return settings;
                lock (_staticLocker)
                {
                    if (_settings != null)
                        return _settings;
                    _settings = new Settings();
                    return _settings;
                }
            }
        }

        private volatile BackgroundTaskManager _backgroundTaskManager;
        public BackgroundTaskManager BackgroundTaskManager
        {
            get
            {
                var btm = _backgroundTaskManager;
                if (btm != null)
                    return btm;
                return _backgroundTaskManager = InitBackgroundTaskManager();
            }
        }

        private BackgroundTaskManager InitBackgroundTaskManager()
        {
            lock (_staticLocker)
            {
                if (_backgroundTaskManager != null)
                    return _backgroundTaskManager;
                _backgroundTaskManager = new BackgroundTaskManager(ViewManager);
                _backgroundTaskManager.RegisterDiscardable(Constants.Tasks.LoadChangesetsTaskKey);
                _backgroundTaskManager.RegisterDiscardable(Constants.Tasks.LoadWorkItemsTaskKey);
                _backgroundTaskManager.RegisterDiscardable(Constants.Tasks.LoadRootQueryTaskKey);
                _backgroundTaskManager.RegisterDiscardable(Constants.Tasks.LoadCompleteBranchListTaskKey);
                _backgroundTaskManager.RegisterDiscardable(Constants.Tasks.LoadAllAssociatedChangesetsIncludingMergesKey);
                _backgroundTaskManager.RegisterDiscardable(Constants.Tasks.CheckTfsUserTaskKey);
                return _backgroundTaskManager;
            }
        }

        private volatile ViewManager _viewManager;
        public ViewManager ViewManager
        {
            get
            {
                var vm = _viewManager;
                if (vm != null)
                    return vm;
                lock (_staticLocker)
                {
                    if (_viewManager != null)
                        return _viewManager;
                    _viewManager = new ViewManager();
                    return _viewManager;
                }
            }
        }

        private volatile ProfileProvider _profileProvider;
        public ProfileProvider ProfileProvider
        {
            get
            {
                var pp = _profileProvider;
                if (pp != null)
                    return pp;
                lock (_staticLocker)
                {
                    if (_profileProvider != null)
                        return _profileProvider;
                    _profileProvider = new ProfileProvider();
                    return _profileProvider;
                }
            }
        }
    }
}
