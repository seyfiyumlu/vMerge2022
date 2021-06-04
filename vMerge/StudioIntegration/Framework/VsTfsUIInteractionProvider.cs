using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using Microsoft.VisualStudio.TeamFoundation.WorkItemTracking;
using alexbegh.Utility.Helpers.Logging;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;
using alexbegh.vMerge.StudioIntegration.ToolWindows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;

namespace alexbegh.vMerge.StudioIntegration.Framework
{
    public class VsTfsUIInteractionProvider : ITfsUIInteractionProvider
    {
        public EnvDTE.DTE Dte
        {
            get;
            private set;
        }

        public IVsUIShell UIShell
        {
            get;
            set;
        }

        public VsTfsUIInteractionProvider(EnvDTE.DTE dte)
        {
            Dte = dte;
        }

        public void ShowWorkItem(int id)
        {
            try
            {
                DocumentService doc = Dte.GetObject("Microsoft.VisualStudio.TeamFoundation.WorkItemTracking.DocumentService") as DocumentService;
                IWorkItemDocument wiDoc = doc.GetWorkItem(Repository.Instance.TfsBridgeProvider.TfsTeamProjectCollection, id, this);
                try
                {
                    if (!wiDoc.IsLoaded)
                        wiDoc.Load();
                    doc.ShowWorkItem(wiDoc);
                }
                finally
                {
                    wiDoc.Release(this);
                }
            }
            catch (Exception)
            {
            }
        }

        public void ShowChangeset(int id)
        {
            try
            {
                var ext = Dte.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
                ext.ViewChangesetDetails(id);
            }
            catch (Exception)
            {
            }
        }

        public void TrackWorkItem(int id)
        {
            try
            {
                var ext = Dte.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
                ext.BranchVisualizer.TrackWorkItem(id);
            }
            catch (Exception)
            {
            }
        }

        public void TrackChangeset(int id)
        {
            try
            {
                var ext = Dte.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
                ext.BranchVisualizer.TrackChangeset(id);
            }
            catch (Exception)
            {
            }
        }

        enum GetAncestorFlags
        {
            /// <summary>
            /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function. 
            /// </summary>
            GetParent = 1,
            /// <summary>
            /// Retrieves the root window by walking the chain of parent windows.
            /// </summary>
            GetRoot = 2,
            /// <summary>
            /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent. 
            /// </summary>
            GetRootOwner = 3
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Retrieves the handle to the ancestor of the specified window. 
        /// </summary>
        /// <param name="hwnd">A handle to the window whose ancestor is to be retrieved. 
        /// If this parameter is the desktop window, the function returns NULL. </param>
        /// <param name="flags">The ancestor to be retrieved.</param>
        /// <returns>The return value is the handle to the ancestor window.</returns>
        [DllImport("user32.dll", ExactSpelling = true)]
        static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        /// <summary>The GetForegroundWindow function returns a handle to the foreground window.</summary>
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        private Dictionary<IVsWindowFrame, bool> _attachedFrames = new Dictionary<IVsWindowFrame, bool>();

        public bool ShowDifferencesPerTF(string rootPath, string sourcePath, string targetPath)
        {
            try
            {
                _attachedFrames.Clear();
                string pathToTools = Environment.GetEnvironmentVariable("VSAPPIDDIR");
                if (String.IsNullOrEmpty(pathToTools)) throw new Exception("tf.exe not found");
                string pathToTF = Path.GetFullPath(Path.Combine(pathToTools, "..\\IDE\\CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer\\TF.exe"));


                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = rootPath;
                psi.Arguments = String.Format("diff \"{0}\" \"{1}\"", sourcePath, targetPath);
                psi.FileName = pathToTF;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                bool hasSet = false;

                while (!p.WaitForExit(10))
                {
                    SetMergeDiffWindowsToFloatOnly();

                    bool setForeground = true;
                    foreach (EnvDTE.Window w1 in Dte.Windows)
                    {
                        EnvDTE80.Window2 window = w1 as EnvDTE80.Window2;
                        if (window.Caption.StartsWith("Merge - ") || window.Caption.StartsWith("Diff - "))
                        {
                            if (!hasSet || window.WindowState != EnvDTE.vsWindowState.vsWindowStateMaximize)
                            {
                                hasSet = true;
                                window.IsFloating = true;
                                window.WindowState = EnvDTE.vsWindowState.vsWindowStateMaximize;
                            }
                            setForeground = false;
                        }
                    }
                    if (setForeground)
                    {
                        var foregroundWindow = GetForegroundWindow();
                        var mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
                        uint foregroundProcess = 0;
                        uint mainWindowProcess = 0;
                        GetWindowThreadProcessId(foregroundWindow, out foregroundProcess);
                        GetWindowThreadProcessId(mainWindowHandle, out mainWindowProcess);

                        if (foregroundProcess == mainWindowProcess)
                        {
                            SetForegroundWindow(p.MainWindowHandle);
                        }
                    }
                    Application.DoEvents();
                }

                foreach (var frame in _attachedFrames.Keys)
                    frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, null);
                _attachedFrames.Clear();

                if (p.ExitCode == 100)
                    return false;
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true);
            }
            return true;
       }

        public void ResolveConflictsPerTF(string rootPath)
        {
            try
            {
                _attachedFrames.Clear();
                string pathToTools = Environment.GetEnvironmentVariable("VSAPPIDDIR");
                if (String.IsNullOrEmpty(pathToTools)) throw new Exception("tf.exe not found");
                string pathToTF = Path.GetFullPath(Path.Combine(pathToTools, "..\\IDE\\CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer\\TF.exe"));


                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = rootPath;
                psi.Arguments = "resolve /recursive /prompt /overridetype:utf-8";
                psi.FileName = pathToTF;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                bool hasSet = false;

                while (!p.WaitForExit(10))
                {
                    SetMergeDiffWindowsToFloatOnly();

                    bool setForeground = true;
                    foreach (EnvDTE.Window w1 in Dte.Windows)
                    {
                        EnvDTE80.Window2 window = w1 as EnvDTE80.Window2;
                        if (window.Caption.StartsWith("Merge - ") || window.Caption.StartsWith("Diff - "))
                        {
                            if (!hasSet || window.WindowState != EnvDTE.vsWindowState.vsWindowStateMaximize)
                            {
                                hasSet = true;
                                window.IsFloating = true;
                                window.WindowState = EnvDTE.vsWindowState.vsWindowStateMaximize;
                            }
                            setForeground = false;
                        }
                    }
                    if (setForeground)
                    {
                        var foregroundWindow = GetForegroundWindow();
                        var mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
                        uint foregroundProcess = 0;
                        uint mainWindowProcess = 0;
                        GetWindowThreadProcessId(foregroundWindow, out foregroundProcess);
                        GetWindowThreadProcessId(mainWindowHandle, out mainWindowProcess);

                        if (foregroundProcess == mainWindowProcess)
                        {
                            SetForegroundWindow(p.MainWindowHandle);
                        }
                    }
                    Application.DoEvents();
                }

                foreach(var frame in _attachedFrames.Keys)
                    frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, null);
                _attachedFrames.Clear();
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true);
            }
        }

        public bool ResolveConflictsInternally(ITfsTemporaryWorkspace workspace)
        {
            try
            {
                var MicrosoftVisualStudioTeamFoundationVersionControlControlsDll =
                    AppDomain.CurrentDomain.GetAssemblies().Where(ass => ass.GetName().Name == "Microsoft.TeamFoundation.VersionControl.Controls").FirstOrDefault();
                if (MicrosoftVisualStudioTeamFoundationVersionControlControlsDll == null)
                    return false;

                var resolveConflictsManagerType = MicrosoftVisualStudioTeamFoundationVersionControlControlsDll.GetTypes().Where(type => type.Name == "ResolveConflictsManager" && type.Namespace == "Microsoft.VisualStudio.TeamFoundation.VersionControl").FirstOrDefault();
                if (resolveConflictsManagerType == null)
                    return false;

                dynamic mgr = Activator.CreateInstance(resolveConflictsManagerType);
                mgr.Initialize();
                mgr.ResolveConflicts((Workspace)workspace.TfsWorkspace, new string[] { workspace.MappedFolder }, true, false);
                mgr.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex, true);
                return false;
            }
        }

        private void SetMergeDiffWindowsToFloatOnly()
        {
            if (vMergePackage.MergeToolWindowIsVisible)
                return;

            IVsWindowFrame[] frames = new IVsWindowFrame[1];
            uint numFrames;
            IEnumWindowFrames ppenum;
            UIShell.GetDocumentWindowEnum(out ppenum);

            while (ppenum.Next(1, frames, out numFrames) == VSConstants.S_OK && numFrames == 1)
            {
                var frame = frames[0] as IVsWindowFrame;
                object title;
                frame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out title);
                var window = VsShellUtilities.GetWindowObject(frame);

                if (window.Caption.StartsWith("Merge - ") || window.Caption.StartsWith("Diff - "))
                {
                    if (!_attachedFrames.ContainsKey(frame))
                    {
                        window.IsFloating = true;
                        var windowEvents = new ToolWindowEvents(frame);
                        windowEvents.OnDockableChange = (fDockable, x, y, w, h) =>
                            {
                                if (_attachedFrames.ContainsKey(frame) && !_attachedFrames[frame])
                                {
                                    try
                                    {
                                        _attachedFrames[frame] = true;
                                        if (fDockable)
                                            window.IsFloating = true;
                                    }
                                    catch (Exception)
                                    {
                                    }
                                    finally
                                    {
                                        _attachedFrames[frame] = false;
                                    }
                                }
                                return VSConstants.S_OK;
                            };
                        windowEvents.OnClose = (options) =>
                            {
                                windowEvents.OnDockableChange = null;
                                windowEvents.OnClose = null;
                                _attachedFrames.Remove(frame);
                                windowEvents.Dispose();
                                return VSConstants.S_OK;
                            };
                        try
                        {
                            frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, windowEvents);
                            _attachedFrames[frame] = false;
                        }
                        catch (Exception ex)
                        {
                            SimpleLogger.Log(ex, true);
                        }
                    }
                }
            }
        }
        
        public string BrowseForTfsFolder(string startFrom)
        {
            var ext = Dte.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;

            VersionControlServer versionControlServer = Repository.Instance.TfsBridgeProvider.VersionControlServer;
            Assembly controlsAssembly = Assembly.GetAssembly(typeof(Microsoft.TeamFoundation.VersionControl.Controls.ControlAddItemsExclude));
            Type vcChooseItemDialogType = controlsAssembly.GetType("Microsoft.TeamFoundation.VersionControl.Controls.DialogChooseItem");

            ConstructorInfo ci = vcChooseItemDialogType.GetConstructor(
                               BindingFlags.Instance | BindingFlags.NonPublic,
                               null,
                               new Type[] { typeof(VersionControlServer), typeof(string), typeof(string) },
                               null);
            var chooseItemDialog = (Form)ci.Invoke(new object[] { versionControlServer, startFrom, startFrom });

            var selectedItemProperty = vcChooseItemDialogType.GetProperty("SelectedItem", BindingFlags.Instance | BindingFlags.NonPublic);
            if (chooseItemDialog.ShowDialog() == DialogResult.OK)
            {
                var itemResult = (Item)selectedItemProperty.GetValue(chooseItemDialog, null);
                return itemResult.ServerItem;
            }
            return null;
        }
    }
}
