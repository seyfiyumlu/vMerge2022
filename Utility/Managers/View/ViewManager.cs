using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Interop;
using System.Windows.Input;
using alexbegh.Utility.Helpers.Logging;

namespace alexbegh.Utility.Managers.View
{
    /// <summary>
    /// The IoC manager wiring ViewModels and Views.
    /// Checks loaded assemblies for View classes which have the
    /// [AssociatedViewModel(ViewModelType)] attribute set.
    /// Provides a way to create a matching view
    /// given an instance of a ViewModel.
    /// </summary>
    public class ViewManager
    {
        #region Private Classes
        /// <summary>
        /// This class bundles all information to a View class
        /// </summary>
        private class TargetView
        {
            /// <summary>
            /// The assembly the view is located in
            /// </summary>
            public Assembly Assembly { get; set; }

            /// <summary>
            /// The AssociatedViewModelAttribute belonging to the view
            /// </summary>
            public AssociatedViewModelAttribute AssociatedViewModel { get; set; }

            /// <summary>
            /// The View type
            /// </summary>
            public Type View { get; set; }

            /// <summary>
            /// The ViewModel type
            /// </summary>
            public Type ViewModel { get { return AssociatedViewModel.AssociatedViewModel; } }

            /// <summary>
            /// Provides advanced override behavior
            /// </summary>
            public Func<object, AbstractView> CustomCreateMethod { get; set; }
        };
        #endregion

        #region Private Fields
        /// <summary>
        /// Map ViewModel types to matching Views
        /// </summary>
        private Dictionary<Type, List<TargetView>> _viewModelToViewMappings;

        /// <summary>
        /// List of unmanaged parent windows to use for modal dialogs
        /// </summary>
        private List<System.Windows.Forms.UserControl> _unmanagedParents;
        #endregion

        #region Private Methods
        /// <summary>
        /// Query loaded assemblies for matching views
        /// </summary>
        private void BuildOrUpdateAttributeDictionary()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            _viewModelToViewMappings = new Dictionary<Type, List<TargetView>>();

            // Register all views
            foreach (var assembly in loadedAssemblies)
            {
                try
                {
                    // Find all views having the AssociatedViewModel attribute
                    var viewTypes
                        = assembly.GetTypes()
                            .Where(type => type.GetCustomAttributes(typeof(AssociatedViewModelAttribute), false).Any())
                            .Select(type =>
                                new TargetView()
                                {
                                    Assembly = assembly,
                                    AssociatedViewModel = (AssociatedViewModelAttribute)type.GetCustomAttributes(typeof(AssociatedViewModelAttribute), false).First(),
                                    View = type
                                });

                    // Build the dictionary
                    foreach (var viewType in viewTypes)
                    {
                        var viewModel = viewType.ViewModel;
                        List<TargetView> viewList = null;
                        if (!_viewModelToViewMappings.TryGetValue(viewModel, out viewList))
                        {
                            viewList = new List<TargetView>();
                            _viewModelToViewMappings[viewModel] = viewList;
                        }

                        viewList.Add(viewType);
                    }
                }
                catch (Exception ex)
                {

                    SimpleLogger.Log(SimpleLogLevel.Error, "Couldn't load assembly " + assembly.ToString());
                    SimpleLogger.Log(SimpleLogLevel.Error, ex.ToString());
                }
            }
        }

        /// <summary>
        /// Set wpf window owner for modal windows
        /// </summary>
        /// <param name="window">The modal wpf window</param>
        private void SetUnmanagedOwnerIfNecessary(Window window)
        {
            if (_unmanagedParents == null || _unmanagedParents.Count == 0)
                return;

            var interopHelper = new WindowInteropHelper(window)
            {
                Owner = _unmanagedParents.Last().Handle
            };
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add the given unmanaged window as modal parent
        /// </summary>
        /// <param name="parent">The unmanaged window</param>
        public void AddUnmanagedParent(System.Windows.Forms.UserControl parent)
        {
            if (_unmanagedParents == null)
                _unmanagedParents = new List<System.Windows.Forms.UserControl>();
            _unmanagedParents.Add(parent);
        }

        /// <summary>
        /// Remove the unmanaged window as parent
        /// </summary>
        /// <param name="parent">The unmanaged window</param>
        public void RemoveUnmanagedParent(System.Windows.Forms.UserControl parent)
        {
            if (_unmanagedParents == null)
                return;
            _unmanagedParents.Remove(parent);
        }

        /// <summary>
        /// Checks if an unmanaged parent window is in the parent window chain
        /// </summary>
        /// <returns>true if so</returns>
        public bool IsUnmanagedParentOnTop()
        {
            if (_unmanagedParents == null)
                return false;
            return _unmanagedParents.Count != 0;
        }

        /// <summary>
        /// Allows to customize the creation of a specific view for a certain viewmodel
        /// </summary>
        /// <typeparam name="Type">The type of the viewmodel</typeparam>
        /// <param name="isModal">Modal window will be created?</param>
        /// <param name="isDefault">Default?</param>
        /// <param name="customCreateMethod">The create method</param>
        /// <param name="key">The create key</param>
        /// <param name="overrideExisting">true if an  existing mapping should be overridden</param>
        public void ProvideFactoryMethodFor<Type>(bool isModal, bool isDefault, Func<object, AbstractView> customCreateMethod, string key = null, bool overrideExisting = false)
        {
            if (_viewModelToViewMappings == null)
                BuildOrUpdateAttributeDictionary();

            List<TargetView> targetViews;
            if (_viewModelToViewMappings.TryGetValue(typeof(Type), out targetViews))
            {
                var matchingExisting = targetViews.Where(item => item.AssociatedViewModel.Key == key).FirstOrDefault();
                if (matchingExisting != null)
                {
                    if (!overrideExisting)
                        throw new ArgumentException("This mapping already exists, set override to true if intended");
                    targetViews.Remove(matchingExisting);
                }
            }
            else
            {
                targetViews = new List<TargetView>();
                _viewModelToViewMappings[typeof(Type)] = targetViews;
            }

            var newView = new TargetView();
            newView.AssociatedViewModel = new AssociatedViewModelAttribute(typeof(Type)) { Key = key, IsModal = isModal, IsDefault = isDefault };
            newView.Assembly = null;
            newView.View = null;
            newView.CustomCreateMethod = customCreateMethod;
            targetViews.Add(newView);
        }

        /// <summary>
        /// Waits for a finishable view model to finish, maintaining UI accessibility
        /// </summary>
        /// <param name="viewModel">The viewmodel to wait for</param>
        public void WaitForFinish(IViewModelIsFinishable viewModel)
        {
            bool finished = false;
            viewModel.Finished += (o, a) => finished = true;
            while (!finished)
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Create a matching view for a given ViewModel instance
        /// </summary>
        /// <typeparam name="Type">The type of the ViewModel</typeparam>
        /// <param name="viewModel">The instance of the ViewModel</param>
        /// <param name="key">The View identifiert (see AssociatedViewModelAttribute)</param>
        /// <returns>An AbstractView instance if successful, null otherwise</returns>
        public AbstractView CreateViewFor<Type>(Type viewModel, string key = null)
        {
            if (_viewModelToViewMappings == null
                || !_viewModelToViewMappings.ContainsKey(typeof(Type)))
            {
                BuildOrUpdateAttributeDictionary();
            }

            List<TargetView> possibleViews = null;
            if (!_viewModelToViewMappings.TryGetValue(typeof(Type), out possibleViews))
            {
                throw new ArgumentException("Unknown ViewModel type");
            }

            TargetView selectedView = null;
            if (key == null)
            {
                if (possibleViews.Count == 1)
                {
                    selectedView = possibleViews.First();
                }
                else
                {
                    var defaultView = possibleViews.Where(item => item.AssociatedViewModel.IsDefault).FirstOrDefault();
                    if (defaultView != null)
                        selectedView = defaultView;
                    else
                        selectedView = possibleViews.FirstOrDefault();
                }
            }
            else
            {
                selectedView = possibleViews.Where(item => item.AssociatedViewModel.Key == key).FirstOrDefault();
            }

            if (selectedView.CustomCreateMethod != null)
            {
                return selectedView.CustomCreateMethod(viewModel);
            }

            ContentControl result = null;
            if (selectedView.AssociatedViewModel.FactoryMethod != null)
                result = selectedView.AssociatedViewModel.FactoryMethod();
            else
                result = Activator.CreateInstance(selectedView.View) as ContentControl;

            if (result != null)
            {
                result.Loaded += (sender, e) =>
                    result.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

                result.DataContext = viewModel;
            }

            if (selectedView.AssociatedViewModel.IsModal)
            {
                SynchronizationContext.Current.Post(
                    (o) =>
                    {
                        if (BeforeModalDialog != null)
                            BeforeModalDialog(result, null);
                        if (WindowCreated != null)
                            WindowCreated(result, null);
                        SetUnmanagedOwnerIfNecessary(result as Window);
                        (result as Window).ShowDialog();
                        if (AfterModalDialog != null)
                            AfterModalDialog(result, null);
                    }, null);
            }
            else
            {
                if (WindowCreated != null)
                    WindowCreated(result, null);
            }

            return new AbstractView(result);
        }

        /// <summary>
        /// Create a matching view for a given ViewModel instance
        /// </summary>
        /// <typeparam name="Type">The type of the ViewModel</typeparam>
        /// <param name="viewModel">The instance of the ViewModel</param>
        /// <param name="key">The View identifiert (see AssociatedViewModelAttribute)</param>
        /// <returns>An AbstractView instance if successful, null otherwise</returns>
        public AbstractView ShowModal<Type>(Type viewModel, string key = null)
        {
            AbstractView abstractView = null;
            if (_viewModelToViewMappings == null
                || !_viewModelToViewMappings.ContainsKey(typeof(Type)))
            {
                BuildOrUpdateAttributeDictionary();
            }

            List<TargetView> possibleViews = null;
            if (!_viewModelToViewMappings.TryGetValue(typeof(Type), out possibleViews))
                throw new ArgumentException("Unknown ViewModel type");

            possibleViews = possibleViews.Where(view => view.AssociatedViewModel.IsModal).ToList();
            if (possibleViews.Count == 0)
                throw new ArgumentException("No modal view registered for this type of view model");

            TargetView selectedView = null;
            if (key == null)
            {
                if (possibleViews.Count == 1)
                {
                    selectedView = possibleViews.First();
                }
                else
                {
                    var defaultView = possibleViews.Where(item => item.AssociatedViewModel.IsDefault).FirstOrDefault();
                    if (defaultView != null)
                        selectedView = defaultView;
                    else
                        selectedView = possibleViews.FirstOrDefault();
                }
            }
            else
            {
                selectedView = possibleViews.Where(item => item.AssociatedViewModel.Key == key).FirstOrDefault();
            }

            if (selectedView.CustomCreateMethod != null)
            {
                abstractView = selectedView.CustomCreateMethod(viewModel);
            }
            else
            {
                ContentControl result = null;
                if (selectedView.AssociatedViewModel.FactoryMethod != null)
                    result = selectedView.AssociatedViewModel.FactoryMethod();
                else
                    result = Activator.CreateInstance(selectedView.View) as ContentControl;

                if (result != null)
                    result.DataContext = viewModel;

                abstractView = new AbstractView(result);
            }

            abstractView.View.Loaded += (sender, e) =>
                abstractView.View.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            if (BeforeModalDialog != null)
                BeforeModalDialog(abstractView.View, null);
            if (WindowCreated != null)
                WindowCreated(abstractView.View, null);
            SetUnmanagedOwnerIfNecessary(abstractView.View as Window);
            (abstractView.View as Window).ShowDialog();
            if (AfterModalDialog != null)
                AfterModalDialog(abstractView.View, null);

            return abstractView;
        }

        /// <summary>
        /// Browse a folder
        /// </summary>
        /// <param name="selected">The folder to start from, null otherwise</param>
        /// <returns>null if cancelled, folder name otherwise</returns>
        public string BrowseForFolder(string selected = null)
        {
            using (var folderDialog = new CommonOpenFileDialog())
            {
                folderDialog.IsFolderPicker = true;
                folderDialog.InitialDirectory = selected;
                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    return folderDialog.FileName;
                }
                return null;
            }
        }
        #endregion

        #region Public Events
        /// <summary>
        /// Called before a modal dialog is being displayed
        /// </summary>
        public event EventHandler BeforeModalDialog;

        /// <summary>
        /// Called after a modal dialog has been closed
        /// </summary>
        public event EventHandler AfterModalDialog;

        /// <summary>
        /// Called when a new window has been created
        /// </summary>
        public event EventHandler WindowCreated;
        #endregion

        #region Internal Operations
        /// <summary>
        /// Raises BeforeModalDialog
        /// </summary>
        /// <param name="wnd">The window</param>
        public void RaiseBeforeModalDialog(Window wnd)
        {
            if (BeforeModalDialog != null)
                BeforeModalDialog(wnd, null);
        }
        /// <summary>
        /// Raises AfterModalDialog
        /// </summary>
        /// <param name="wnd">The window</param>
        public void RaiseAfterModalDialog(Window wnd)
        {
            if (AfterModalDialog != null)
                AfterModalDialog(wnd, null);
        }
        /// <summary>
        /// Raises WindowCreated
        /// </summary>
        /// <param name="wnd">The window</param>
        public void RaiseWindowCreated(Window wnd)
        {
            if (WindowCreated != null)
                WindowCreated(wnd, null);
        }
        #endregion
    }
}
