using alexbegh.Utility.Managers.View;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.ViewModel.Merge;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace alexbegh.vMerge.View
{
    /// <summary>
    /// Interaction logic for PrepareMergeDialogWindow.xaml
    /// </summary>
    [AssociatedViewModel(typeof(PrepareMergeViewModel), Key="Embedded")]
    public partial class PrepareMergeWindow : UserControl
    {
        public PrepareMergeWindow()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            Loaded += (s, e) =>
                {
                    var data = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.PrepareMergeDialogWindowViewSettingsKey);
                    this.DeserializeFromString(data, false);
                    Window.GetWindow(this).Closing += PrepareMergeWindow_Closing;
                    var vm = (DataContext as PrepareMergeViewModel);
                    vm.SelectNewRowAction = () => Repository.Instance.BackgroundTaskManager.Send(() => { SelectNextRow(); return true; });
                };
            Unloaded += (s,e) =>
                {
                    var data = this.SerializeToString();
                    Repository.Instance.Settings.SetSettings(Constants.Settings.PrepareMergeDialogWindowViewSettingsKey, data);
                };
        }

        void PrepareMergeWindow_Closing(object sender, EventArgs e)
        {
            var data = Window.GetWindow(this).SerializeToString();
            Repository.Instance.Settings.SetSettings(Constants.Settings.PrepareMergeDialogWindowViewSettingsKey, data);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            FocusManager.SetFocusedElement(this, ChangesetsTableLoader);
        }

        private void ChangesetsTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = (DataContext as PrepareMergeViewModel);
            var changeset = (e.OriginalSource as FrameworkElement);
            vm.PerformMergeCommand.Execute(changeset.DataContext);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var vm = (DataContext as PrepareMergeViewModel);
                var changeset = (e.OriginalSource as FrameworkElement);
                if (vm != null && changeset != null)
                {
                    vm.PerformMergeCommand.Execute(changeset.DataContext);
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        private void ChangesetsGridLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            ChangesetsGrid = sender as DataGrid;
            var vm = DataContext as PrepareMergeViewModel;
            BindToChangesetChanges();

            vm.PropertyChanged +=
                (o, a) =>
                {
                    if (a.PropertyName == "ChangesetList")
                    {
                        BindToChangesetChanges();
                    }
                };
        }

        private void BindToChangesetChanges()
        {
            var vm = DataContext as PrepareMergeViewModel;
            if (vm.ChangesetList != null)
            {
                foreach (var item in vm.ChangesetList)
                {
                    item.PropertyChanged +=
                        (o, a) =>
                        {
                            if (a.PropertyName == "TargetCheckinId")
                            {
                                Repository.Instance.BackgroundTaskManager.Post(
                                    () =>
                                    {
                                        ChangesetsGrid.ScrollIntoView(o as PrepareMergeViewModel.ChangesetListElement, ChangesetsGrid.Columns.Where(col => col.SortMemberPath == "TargetCheckinId").FirstOrDefault());
                                        return true;
                                    }
                                );
                            }
                        };
                }
            }
        }

        private void SelectNextRow()
        {
            if (ChangesetsGrid.SelectedIndex >= 0
                && ChangesetsGrid.SelectedIndex < (ChangesetsGrid.Items.Count - 1))
            {
                ChangesetsGrid.Focus();
                ++ChangesetsGrid.SelectedIndex;
            }
        }

        private DataGrid ChangesetsGrid
        {
            get;
            set;
        }
    }
}
