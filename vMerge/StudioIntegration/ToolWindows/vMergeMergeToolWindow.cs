using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using alexbegh.vMerge.ViewModel.WorkItems;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.StudioIntegration.Framework;
using alexbegh.vMerge.ViewModel;
using alexbegh.vMerge.ViewModel.Merge;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Controls;
using qbusSRL.vMerge;

namespace alexbegh.vMerge.StudioIntegration
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>s
    [Guid("ec10ec1c-57aa-4402-8e89-ed7a2b73ae4a")]
    public class vMergeMergeToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public vMergeMergeToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.MergeToolWindowTitle;
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            var mergeViewModel = vMergePackage.PrepareMergeViewModel;
            var mergeWindow = Repository.Instance.ViewManager.CreateViewFor(mergeViewModel, "Embedded");
            vMergePackage.ThemeWindow(mergeWindow.View);
            //MahApps.Metro.ThemeManager.ChangeTheme(mergeWindow.View.Resources, vMergePackage.DefaultAccent, vMergePackage.DefaultTheme);
            base.Content = mergeWindow.View;
            mergeViewModel.Finished += ViewModelClosed;
            vMergePackage.SetMergeToolWindowIsVisible();

            vMergePackage.PrepareMergeViewModelChanged += (o, a) =>
                {
                    if (mergeWindow.View.DataContext != null)
                        ((PrepareMergeViewModel)mergeWindow.View.DataContext).Finished -= ViewModelClosed;
                    mergeWindow.View.DataContext = vMergePackage.PrepareMergeViewModel;
                    vMergePackage.PrepareMergeViewModel.Finished += ViewModelClosed;
                };
        }

        private void ViewModelClosed(object sender, Utility.Managers.View.ViewModelFinishedEventArgs e)
        {
            vMergePackage.SetMergeToolWindowIsVisible(false);
            ((PrepareMergeViewModel)((ContentControl)Content).DataContext).Finished -= ViewModelClosed;
            IVsWindowFrame windowFrame = (IVsWindowFrame)Frame;
            windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_SaveIfDirty);
        }

        public override int LoadUIState(System.IO.Stream stateStream)
        {
            return base.LoadUIState(stateStream);
        }
    }
}
