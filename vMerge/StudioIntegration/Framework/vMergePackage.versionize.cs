using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using alexbegh.Utility.Commands;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using alexbegh.vMerge.StudioIntegration.Options;
using alexbegh.vMerge.StudioIntegration.ToolWindows;
using alexbegh.vMerge.ViewModel;
using alexbegh.vMerge.ViewModel.Merge;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using alexbegh.Utility.Helpers.Logging;
using System.Drawing;
using alexbegh.Utility.Helpers.WeakReference;
using alexbegh.vMerge.View;
using System.Windows.Forms;
using alexbegh.Utility.Helpers.ViewModel;
using qbusSRL.vMerge;

namespace alexbegh.vMerge.StudioIntegration.Framework
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "{{VERSION}}", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(vMergeWorkItemsToolWindow))]
    [ProvideToolWindow(typeof(vMergeChangesetsToolWindow))]
    [ProvideToolWindow(typeof(vMergeMergeToolWindow), Transient = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.EmptySolution_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string)]
    [ProvideAutoLoad("{e13eedef-b531-4afe-9725-28a69fa4f896}")]
    [ProvideOptionPage(typeof(vMergeOptionsPage), "vMerge", "Options", 113, 114, true)]
    [ProvideOptionPage(typeof(vMergeProfilesPage), "vMerge", "Profiles", 113, 115, true)]
    [ProvideBindingPath]
    [Guid(GuidList.guidvMergePkgString)]
    public sealed class vMergePackage : Package, IVsSelectionEvents
    {
        private EnvDTE80.Events2 _events;

        private static WeakReferenceList<ContentControl> _themedControls = new WeakReferenceList<ContentControl>();

        private static TfsItemCache _tfsItemCache;
        internal static TfsItemCache TfsItemCache
        {
            get
            {
                if (_tfsItemCache == null)
                {
                    Repository.SetUIThread();
                    _tfsItemCache = new TfsItemCache();
                }
                return _tfsItemCache;
            }
        }

        private static event EventHandler _mergeToolWindowVisibilityChanged;
        public static event EventHandler MergeToolWindowVisibilityChanged
        {
            add { _mergeToolWindowVisibilityChanged += value; }
            remove { _mergeToolWindowVisibilityChanged -= value; }
        }

        public static bool MergeToolWindowIsVisible
        {
            get;
            private set;
        }

        public static void SetMergeToolWindowIsVisible(bool visible = true)
        {
            MergeToolWindowIsVisible = visible;
            if (_mergeToolWindowVisibilityChanged != null)
            {
                _mergeToolWindowVisibilityChanged(null, new EventArgs());
            }
        }

        private class ResourceDictionaryResult
        {
            public ResourceDictionary Dic { get; set; }
            public object Key { get; set; }
            public object Value { get; set; }
        }

        private static ResourceDictionary _themeDic, _baseThemeDic;
        public static void ThemeWindow(ContentControl control)
        {
            if (control.GetType().GetCustomAttributes(typeof(DoNotStyleAttribute), false).Any())
            {
                return;
            }
            _themedControls.Compact();
            _themedControls.Add(control);
            if (_themeDic == null)
                SetTheme();
            if (!control.Resources.MergedDictionaries.Contains(_themeDic))
                control.Resources.MergedDictionaries.Add(_themeDic);
            if (!control.Resources.MergedDictionaries.Contains(_baseThemeDic))
                control.Resources.MergedDictionaries.Add(_baseThemeDic);
        }

        private static System.Windows.Forms.Screen GetScreen(Window window)
        {
            return Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(window).Handle);
        }
        private static System.Windows.Point RealPixelsToWpf(Window w, System.Windows.Point p)
        {
            var t = PresentationSource.FromVisual(w).CompositionTarget.TransformFromDevice;
            return t.Transform(p);
        }
        private static void SetPositionBottomRightCorner(Window sourceWindow, Window targetWindow)
        {
            var workingArea = GetScreen(sourceWindow).WorkingArea;
            var corner = RealPixelsToWpf(sourceWindow, new System.Windows.Point(workingArea.Right, workingArea.Bottom));
            targetWindow.Left = corner.X - targetWindow.ActualWidth - 15;
            targetWindow.Top = corner.Y - targetWindow.ActualHeight - 15;
        }

        public static bool IsDarkTheme { get; private set; }

        private bool AreCapsDisabled()
        {
            try
            {
                using (var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\11.0\General", false))
                {
                    if (rk != null)
                    {
                        var val = Convert.ToUInt32(rk.GetValue("SuppressUppercaseConversion"));
                        if (val != 0)
                            return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            try
            {
                using (var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio\12.0\General", false))
                {
                    if (rk != null)
                    {
                        var val = Convert.ToUInt32(rk.GetValue("SuppressUppercaseConversion"));
                        if (val != 0)
                            return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public vMergePackage()
        {
            if (AreCapsDisabled())
            {
                MahApps.Metro.Converters.ToUpperConverter.DisableCaps = true;
                MahApps.Metro.Converters.ToLowerConverter.DisableLower = true;
            }

            SimpleLogger.Init();
            SimpleLogger.Log(SimpleLogLevel.Info, "------------------------------------------------------------------------------\nSession starting");
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            SimpleLogger.Log(SimpleLogLevel.Error, string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            _package = this;

            string targetPath =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vMerge2022",
                    "qbus.vMerge.settings");
            try
            {
                Repository.Initialize(
                    new VsTfsConnectionInfoProvider(),
                    new VsTfsUIInteractionProvider(GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE),
                    new vMergeUIProvider(this));

                Repository.Instance.TfsBridgeProvider.ActiveProjectSelected +=
                    (o, a) =>
                    {
                        if (MergeToolWindowIsVisible)
                        {
                            MergeToolWindowIsVisible = false;
                            if (PrepareMergeViewModel != null)
                                PrepareMergeViewModel.Close();
                            if (_mergeToolWindowVisibilityChanged != null)
                                _mergeToolWindowVisibilityChanged(this, null);

                        }
                    };
                Repository.Instance.ViewManager.BeforeModalDialog +=
                    (o, a) =>
                    {
                        var uiShell = GetService(typeof(SVsUIShell)) as IVsUIShell;
                        var uiShell5 = (IVsUIShell5)uiShell;
                        var wnd = (o as Window);
                        var interopHelper = new System.Windows.Interop.WindowInteropHelper(wnd);
                        IntPtr ownerWnd;
                        uiShell.GetDialogOwnerHwnd(out ownerWnd);
                        var owner = System.Windows.Interop.HwndSource.FromHwnd(ownerWnd);
                        if (owner != null)
                        {
                            wnd.Owner = (owner.RootVisual as Window);
                        }
                        wnd.ShowInTaskbar = false;
                        uiShell.EnableModeless(0);
                        if (interopHelper.Handle != default(IntPtr) && !Repository.Instance.ViewManager.IsUnmanagedParentOnTop())
                            uiShell5.ThemeWindow(interopHelper.Handle);
                    };

                Repository.Instance.ViewManager.WindowCreated +=
                    (o, a) =>
                    {
                        var window = o as ContentControl;
                        ThemeWindow(window);
                    };

                Repository.Instance.ViewManager.AfterModalDialog +=
                    (o, a) =>
                    {
                        var uiShell = GetService(typeof(SVsUIShell)) as IVsUIShell;
                        var wnd = (o as Window);
                        uiShell.EnableModeless(1);
                    };
                try
                {
                    try
                    {
                        Repository.Instance.Settings.LoadSettings(targetPath);
                    }
                    catch (Exception)
                    {
                        Repository.Instance.Settings.LoadSettings(targetPath + ".bak");
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Log(ex);
                }
                SetDefaultSettings();
                Repository.Instance.ProfileProvider.ReloadFromSettings();
                Repository.Instance.Settings.SetAutoSave(targetPath, 500);
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
            }
        }

        private static void SetDefaultSettings()
        {
            if (Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.CheckInCommentTemplateKey) == null)
            {
                Repository.Instance.Settings.SetSettings(Constants.Settings.CheckInCommentTemplateKey, "{SourceComment}\n(vMerge {SourceId} from {SourceBranch})");
            }
            if (!Repository.Instance.Settings.CheckSettingsExist(Constants.Settings.PerformNonModalMergeKey))
            {
                Repository.Instance.Settings.SetSettings(Constants.Settings.PerformNonModalMergeKey, true);
            }
            string basePath = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.LocalWorkspaceBasePathKey);
            if (basePath == null || !Directory.Exists(basePath))
            {
                Repository.Instance.Settings.SetSettings(Constants.Settings.LocalWorkspaceBasePathKey, Path.GetTempPath());
            }
        }

        private static vMergePackage _package;

        private Tuple<double, double, double> Normalize(Color col)
        {
            double max = (double)Math.Max(col.R, Math.Max(col.G, col.B));
            return new Tuple<double, double, double>(((double)col.R) / max, ((double)col.G) / max, ((double)col.B) / max);
        }

        public static void SetTheme(string chosen = null)
        {
            try
            {
                if (chosen == null)
                    chosen = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.SelectedThemeKey);
                var oldTheme = _themeDic;
                var oldBaseTheme = _baseThemeDic;
                IVsUIShell2 uiShell = _package.GetService(typeof(SVsUIShell)) as IVsUIShell2;
                uint rgb;
                uiShell.GetVSSysColorEx((int)__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BACKGROUND, out rgb);
                var col = System.Drawing.ColorTranslator.FromWin32((int)rgb);

                if (col.GetBrightness() < 0.5)
                {
                    IsDarkTheme = true;
                    _themeDic = new System.Windows.ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml") };
                    _baseThemeDic = new System.Windows.ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseDark.xaml") };
                }
                else
                {
                    IsDarkTheme = false;
                    _themeDic = new System.Windows.ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml") };
                    _baseThemeDic = new System.Windows.ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseLight.xaml") };
                }

                if (chosen != null && MahApps.Metro.ThemeManager.Accents.Any(accent => accent.Name == chosen))
                {
                    _themeDic = new System.Windows.ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/" + chosen + ".xaml") };
                }

                foreach (var control in _themedControls)
                {
                    if (control.Resources.MergedDictionaries.Contains(oldTheme))
                        control.Resources.MergedDictionaries.Remove(oldTheme);
                    if (control.Resources.MergedDictionaries.Contains(oldBaseTheme))
                        control.Resources.MergedDictionaries.Remove(oldBaseTheme);
                    if (!control.Resources.MergedDictionaries.Contains(_themeDic))
                        control.Resources.MergedDictionaries.Add(_themeDic);
                    if (!control.Resources.MergedDictionaries.Contains(_baseThemeDic))
                        control.Resources.MergedDictionaries.Add(_baseThemeDic);
                    control.UpdateDefaultStyle();
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
            }
        }

        public void ShowWorkItemView()
        {
            ShowWorkItemsToolWindow(null, null);
        }

        public void ShowChangesetView()
        {
            ShowChangesetsToolWindow(null, null);
        }

        public void RefreshBranches()
        {
            Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                (task) =>
                {
                    Repository.Instance.TfsBridgeProvider.LoadCompleteBranchList();
                }, "Refreshing branch list");
        }

        public static PrepareMergeViewModel PrepareMergeViewModel;
        public static event EventHandler PrepareMergeViewModelChanged;

        public static void OpenMergeView(PrepareMergeViewModel data)
        {
            PrepareMergeViewModel = data;
            if (PrepareMergeViewModelChanged != null)
                PrepareMergeViewModelChanged(_package, new EventArgs());
            _package.ShowMergeToolWindow(null, null);
        }

        public static void ShowMergeView()
        {
            if (!MergeToolWindowIsVisible)
                return;

            _package.ShowMergeToolWindow(null, null);
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowWorkItemsToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(vMergeWorkItemsToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public static void ShowToolWindow(Type typeOfWindow)
        {
            ToolWindowPane window = _package.FindToolWindow(typeOfWindow, 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowChangesetsToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(vMergeChangesetsToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowMergeToolWindow(object sender, EventArgs e)
        {
            IVsUIShell4 uiShell = GetService(typeof(SVsUIShell)) as IVsUIShell4;

            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(vMergeMergeToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            SetMergeToolWindowIsVisible();

            var windowEvents = new ToolWindowEvents(windowFrame);
            windowEvents.OnClose = (options) =>
            {
                MergeToolWindowIsVisible = false;
                if (_mergeToolWindowVisibilityChanged != null)
                    _mergeToolWindowVisibilityChanged(this, new EventArgs());
                windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, null);
                windowEvents.Dispose();
                return VSConstants.S_OK;
            };
            windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, windowEvents);

            vMergeMergeToolWindow mergeToolWindow = (vMergeMergeToolWindow)window;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        private uint cookie;
        private PackageHandlers _handlers;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                base.Initialize();
                SetTheme();

                IVsMonitorSelection monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
                monitorSelection.AdviseSelectionEvents(this, out cookie);

                // Add our command handlers for menu (commands must exist in the .vsct file)
                OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

                if (null != mcs)
                {
                    _handlers = new PackageHandlers();
                    _handlers.Register(mcs);

                    _handlers.SwitchToSettings = new RelayCommand((o) => SwitchToSettings((o as PackageHandlers.ChoiceCommandParameters).Selected as ProfileSelection), (o) => HasProjectAvailableProfiles());
                    _handlers.ShowWorkItemView = new RelayCommand((o) => ShowWorkItemView());
                    _handlers.ShowChangesetView = new RelayCommand((o) => ShowChangesetView());
                    _handlers.RefreshBranches = new RelayCommand((o) => RefreshBranches());
                    _handlers.ShowVMergeHelp = new RelayCommand((o) => ShowVMergeHelp());

                    _handlers.SetTargetBranchChoiceHandler(
                        (args) => GetAvailableMergeTargetBranches());
                    _handlers.TargetBranch = new RelayCommand((o) => MergeToTarget((o as PackageHandlers.ChoiceCommandParameters).Selected as MergeSelection));

                    _handlers.SetTargetBranch2ChoiceHandler(
                        (args) => GetAvailableMergeTargetBranches());
                    _handlers.TargetBranch2 = new RelayCommand((o) => OpenChangesetView((o as PackageHandlers.ChoiceCommandParameters).Selected as MergeSelection));

                    _handlers.SetMatchingProfilesChoiceHandler(
                        (args) => GetMatchingProfilesForBranch());
                    _handlers.MatchingProfiles = new RelayCommand((o) => LoadProfileAndMerge((o as PackageHandlers.ChoiceCommandParameters).Selected as ProfileSelection));

                    _handlers.SetSwitchToSettingsChoiceHandler(
                        (args) => GetMatchingProfilesForProject());

                    _handlers.SetSwitchToSettingsGetSelectedChoiceHandler(
                        (args) => GetActiveProfile());
                }

                var dte = GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                _events = dte.Events as EnvDTE80.Events2;
                _events.WindowVisibilityEvents.WindowHiding += WindowVisibilityEvents_WindowHiding;
                _events.WindowVisibilityEvents.WindowShowing += WindowVisibilityEvents_WindowShowing;

                ((VsTfsUIInteractionProvider)Repository.Instance.TfsUIInteractionProvider).UIShell = GetService(typeof(SVsUIShell)) as IVsUIShell;
                Microsoft.VisualStudio.PlatformUI.VSColorTheme.ThemeChanged += (a) => SetTheme();
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                throw;
            }
        }

        private class MergeSelection
        {
            private ITfsBranch _sourceBranch;
            private ITfsBranch _targetBranch;

            public ITfsBranch SourceBranch
            {
                get
                {
                    return _sourceBranch;
                }
                set
                {
                    _sourceBranch = value;
                }
            }
            public ITfsBranch TargetBranch
            {
                get
                {
                    return _targetBranch;
                }
                set
                {
                    _targetBranch = value;
                }
            }
            public string SourceFilter { get; set; }
            public override string ToString()
            {
                return TargetBranch.ToString();
            }
        }

        private class ProfileSelection
        {
            public IProfileSettings ProfileSettings
            {
                get;
                set;
            }

            public bool IsModified
            {
                get;
                set;
            }

            public override string ToString()
            {
                if (ProfileSettings == null)
                    return "<Modified>";

                return IsModified ? ProfileSettings.Name + " (modified)" : ProfileSettings.Name;
            }
        }

        void MergeToTarget(MergeSelection what)
        {
            try
            {
                IEnumerable<ITfsChangeset> changesets = null;
                if (Repository.Instance.BackgroundTaskManager.RunWithCancelDialog(
                    (trackProgress) =>
                    {
                        trackProgress.TrackProgress.ProgressInfo = "Loading merge candidates from tfs ...";
                        changesets = Repository.Instance.TfsBridgeProvider.GetMergeCandidatesForBranchToBranch(what.SourceBranch, what.TargetBranch, what.SourceFilter);
                    }))
                {
                    PrepareMergeViewModel vm = new PrepareMergeViewModel(_tfsItemCache, changesets);
                    vm.MergeSource = what.SourceBranch;
                    vm.MergeTarget = what.TargetBranch;
                    vm.PathFilter = what.SourceFilter;
                    if (Repository.Instance.Settings.FetchSettings<bool>(Constants.Settings.PerformNonModalMergeKey))
                    {
                        vMergePackage.OpenMergeView(vm);
                    }
                    else
                    {
                        Repository.Instance.ViewManager.ShowModal(vm, "Modal");
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                throw;
            }
        }

        void OpenChangesetView(MergeSelection what)
        {
            try
            {
                if (!what.SourceBranch.IsSubBranch && !Repository.Instance.TfsBridgeProvider.CompleteBranchList.Any(branch => branch.Equals(what.SourceBranch)))
                {
                    var mbvm = new MessageBoxViewModel("Active team project", "The branch you selected belongs to a team project which is currently not active.\nPlease switch to that project in the team explorer pane and try again", MessageBoxViewModel.MessageBoxButtons.OK);
                    Repository.Instance.ViewManager.ShowModal(mbvm);
                    return;
                }
                var settings = Repository.Instance.ProfileProvider.GetDefaultProfile();
                settings.CSSourceBranch = what.SourceBranch.Name;
                settings.CSTargetBranch = what.TargetBranch.Name;
                settings.DateFromFilter = null;
                settings.DateToFilter = null;
                settings.CSQueryName = null;
                settings.ChangesetExcludeCommentFilter = null;
                settings.ChangesetIncludeCommentFilter = null;
                ShowChangesetView();
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                throw;
            }
        }

        void LoadProfileAndMerge(ProfileSelection profileSelection)
        {
            if (!Repository.Instance.TfsBridgeProvider.IsCompleteBranchListLoaded())
                return;

            Repository.Instance.TfsBridgeProvider.ActiveTeamProject =
                Repository.Instance.TfsBridgeProvider.VersionControlServer.GetAllTeamProjects(false)
            .Where(project => project.ArtifactUri.ToString() == profileSelection.ProfileSettings.TeamProject).FirstOrDefault();

            Repository.Instance.ProfileProvider.LoadProfile(Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri, profileSelection.ProfileSettings.Name);
            ShowChangesetView();
        }

        IEnumerable<MergeSelection> GetAvailableMergeTargetBranches()
        {
            if (!Repository.Instance.TfsBridgeProvider.IsCompleteBranchListLoaded())
                return Enumerable.Empty<MergeSelection>();

            // get Team Explorer service
            var dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var ext = dte.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
            if (ext.Explorer.SelectedItems.Length == 1)
            {
                var selectedItem = ext.Explorer.SelectedItems.First();
                var sourceBranch = Repository.Instance.TfsBridgeProvider.GetBranchByNameOrNull(selectedItem.SourceServerPath);
                if (sourceBranch == null)
                {
                    var branches = Repository.Instance.TfsBridgeProvider.CompleteBranchList;
                    sourceBranch
                        = branches
                            .Where(branch => selectedItem.SourceServerPath.StartsWith(branch.Name, StringComparison.InvariantCultureIgnoreCase))
                            .OrderByDescending(branch => branch.Name.Length)
                            .FirstOrDefault();
                }

                if (sourceBranch != null)
                    return
                        Repository.Instance.TfsBridgeProvider.GetPossibleMergeTargetBranches(sourceBranch)
                        .Select(branch => new MergeSelection() { SourceBranch = sourceBranch, TargetBranch = branch, SourceFilter = selectedItem.SourceServerPath })
                        .ToArray();
            }

            return Enumerable.Empty<MergeSelection>();
        }

        IEnumerable<ProfileSelection> GetMatchingProfilesForBranch()
        {
            if (!Repository.Instance.TfsBridgeProvider.IsCompleteBranchListLoaded())
                yield break;

            // get Team Explorer service
            var dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var ext = dte.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
            if (ext.Explorer.SelectedItems.Length == 1)
            {
                var selectedItem = ext.Explorer.SelectedItems.First();
                var branches = Repository.Instance.TfsBridgeProvider.CompleteBranchList;
                var selectedBranch
                    = branches
                        .Where(branch => selectedItem.SourceServerPath.StartsWith(branch.Name, StringComparison.InvariantCultureIgnoreCase))
                        .OrderByDescending(branch => branch.Name.Length)
                        .FirstOrDefault();

                if (selectedBranch != null)
                {
                    var project = Repository.Instance.TfsBridgeProvider.VersionControlServer.GetTeamProjectForServerPath(selectedItem.SourceServerPath);
                    if (project != null)
                    {
                        var profiles = Repository.Instance.ProfileProvider.GetAllProfilesForProject(project.ArtifactUri);
                        foreach (var profile in profiles)
                        {
                            if (profile.CSSourceBranch == selectedBranch.Name
                                || profile.CSTargetBranch == selectedBranch.Name)
                                yield return new ProfileSelection() { ProfileSettings = profile };
                        }
                    }
                }
            }

            yield break;
        }

        private IEnumerable<ProfileSelection> GetMatchingProfilesForProject()
        {
            if (Repository.Instance.TfsBridgeProvider.ActiveTeamProject == null)
                yield break;

            IProfileSettings activeSettings;
            bool alreadyModified;
            Repository.Instance.ProfileProvider.GetActiveProfile(out activeSettings, out alreadyModified);

            if (alreadyModified)
                yield return new ProfileSelection() { ProfileSettings = null };

            foreach (var profile in Repository.Instance.ProfileProvider.GetAllProfilesForProject(Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri))
            {
                yield return new ProfileSelection() { ProfileSettings = profile, IsModified = false };
            }
        }

        private ProfileSelection GetActiveProfile()
        {
            IProfileSettings activeSettings;
            bool alreadyModified;
            Repository.Instance.ProfileProvider.GetActiveProfile(out activeSettings, out alreadyModified);

            if (alreadyModified)
                return new ProfileSelection() { ProfileSettings = null };
            else
                return new ProfileSelection() { ProfileSettings = activeSettings, IsModified = false };
        }

        private bool HasProjectAvailableProfiles()
        {
            if (Repository.Instance.TfsBridgeProvider.ActiveTeamProject == null)
                return false;

            return
                Repository.Instance.ProfileProvider.GetAllProfilesForProject(Repository.Instance.TfsBridgeProvider.ActiveTeamProject.ArtifactUri).Any();
        }

        private void SwitchToSettings(ProfileSelection profileSelection)
        {
            Repository.Instance.ProfileProvider.LoadProfile(new Uri(profileSelection.ProfileSettings.TeamProject), profileSelection.ProfileSettings.Name);
        }

        public static void NavigateToUri(Uri uri)
        {
            IVsWindowFrame ppFrame;
            var service = GetGlobalService(typeof(IVsWebBrowsingService)) as IVsWebBrowsingService;
            service.Navigate(uri.ToString(), 0, out ppFrame);
        }

        private void ShowVMergeHelp()
        {
            NavigateToUri(new Uri("https://github.com/seyfiyumlu/vmerge"));
        }

        void WindowVisibilityEvents_WindowShowing(EnvDTE.Window Window)
        {
            ToolWindowPane window = this.FindToolWindow(typeof(vMergeChangesetsToolWindow), 0, true);
            bool changesetWindowIsOpen = window != null && ((IVsWindowFrame)window.Frame).IsVisible() != 0;
        }

        void WindowVisibilityEvents_WindowHiding(EnvDTE.Window Window)
        {
        }
        #endregion

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            return VSConstants.S_OK;
        }

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }
    }

}
