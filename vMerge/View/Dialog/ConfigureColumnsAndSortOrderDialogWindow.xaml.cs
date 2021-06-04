using Microsoft.VisualStudio.PlatformUI;
using alexbegh.Utility.Helpers.WPFBindings;
using alexbegh.Utility.Managers.View;
using alexbegh.Utility.UserControls.FieldMapperGrid;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.ViewModel.Configuration;
using alexbegh.Utility.SerializationHelpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace alexbegh.vMerge.View.Dialog
{
    /// <summary>
    /// Interaction logic for ConfigureColumnsAndSortOrderDialogWindow.xaml
    /// </summary>
    [AssociatedViewModel(typeof(ConfigureColumnsAndSortOrderViewModel), IsModal=true)]
    public partial class ConfigureColumnsAndSortOrderDialogWindow : DialogWindow
    {
        private class DragDropData
        {
            public FieldMapperGridColumn SourceItem;
            public ListBox SourceControl;
        }

        private Point? _dragStart;

        public ConfigureColumnsAndSortOrderDialogWindow()
        {
            this.Initialized += (o, a) =>
            {
                var data = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.ConfigureColumnsWindowSettingsKey);
                Window.GetWindow(this).DeserializeFromString(data);
            };

            InitializeComponent();
            AvailableItemsListBox.Items.SortDescriptions.Clear();
            AvailableItemsListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Header", System.ComponentModel.ListSortDirection.Ascending));
            AvailableItemsListBox.Items.IsLiveSorting = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var data = ViewSettingsSerializer.SerializeToString(this);
            Repository.Instance.Settings.SetSettings(Constants.Settings.ConfigureColumnsWindowSettingsKey, data);
            base.OnClosing(e);
        }

        private void SelectedItemsListBox_Drop(object sender, DragEventArgs e)
        {
            _dragStart = null;
            var sourceData = (DragDropData)e.Data.GetData(typeof(DragDropData));
            if (sourceData == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            var source = sourceData.SourceControl;
            object sourceVM = (source == AvailableItemsListBox)
                           ? (DataContext as ConfigureColumnsAndSortOrderViewModel).AvailableColumns as object
                           : (DataContext as ConfigureColumnsAndSortOrderViewModel).SelectedColumns as object;

            ListBox parent = sender as ListBox;
            object targetVM = (parent == AvailableItemsListBox)
                           ? (DataContext as ConfigureColumnsAndSortOrderViewModel).AvailableColumns as object
                           : (DataContext as ConfigureColumnsAndSortOrderViewModel).SelectedColumns as object;
            var objectToPlaceBefore = GetObjectDataFromPoint(parent, e.GetPosition(parent)) as FieldMapperGridColumn;
            if (sourceData != null)
            {
                FieldMapperGridColumn showAvailable, showSelected;
                (DataContext as ConfigureColumnsAndSortOrderViewModel).DragDrop(sourceVM, sourceData.SourceItem, targetVM, objectToPlaceBefore, out showAvailable, out showSelected);

                if (showAvailable != null)
                {
                    AvailableItemsListBox.ScrollIntoView(showAvailable);
                    AvailableItemsListBox.SelectedItem = showAvailable;
                }
                if (showSelected != null)
                {
                    SelectedItemsListBox.ScrollIntoView(showSelected);
                    SelectedItemsListBox.SelectedItem = showSelected;
                }
            }
        }

        private void SelectedItemsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
        }

        private void SelectedItemsListBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragStart.HasValue)
                return;

            Vector pos = e.GetPosition(null) - _dragStart.Value;
            if (e.LeftButton == MouseButtonState.Pressed
                && (Math.Abs(pos.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(pos.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                ListBox parent = sender as ListBox;
                var data = GetObjectDataFromPoint(parent, e.GetPosition(parent)) as FieldMapperGridColumn;
                if (data != null)
                {
                    DragDrop.DoDragDrop(parent, new DragDropData() { SourceControl = parent, SourceItem = data }, DragDropEffects.All);
                }
            }
        }

        private static object GetObjectDataFromPoint(ListBox source, Point point)
        {
            UIElement element = source.InputHitTest(point) as UIElement;
            if (element != null)
            {
                object data = DependencyProperty.UnsetValue;
                while (data == DependencyProperty.UnsetValue)
                {
                    data = source.ItemContainerGenerator.ItemFromContainer(element);
                    if (data == DependencyProperty.UnsetValue)
                        element = VisualTreeHelper.GetParent(element) as UIElement;
                    if (element == source)
                        return null;
                }
                if (data != DependencyProperty.UnsetValue)
                    return data;
            }

            return null;
        }

        private void SelectedItemsListBox_DragOver(object sender, DragEventArgs e)
        {
            var sourceData = (DragDropData)e.Data.GetData(typeof(DragDropData));
            if (sourceData == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            var source = sourceData.SourceControl;

            ListBox control =
                DependencyObjectHelper.FindAncestorOrSelf<ListBox>(VisualTreeHelper.HitTest(this, e.GetPosition(this)).VisualHit);

            if (control == AvailableItemsListBox
                || control == SelectedItemsListBox)
            {
                Point point = e.GetPosition(control);
                double topMoveRange = control.ActualHeight / 5;
                double bottomMoveRange = control.ActualHeight - topMoveRange;
                var sv = DependencyObjectHelper.FindVisualChild<ScrollViewer>(control);
                if (point.Y < topMoveRange)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - (topMoveRange - point.Y));
                }
                else if (point.Y > bottomMoveRange)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset + (point.Y - bottomMoveRange));
                }
            }

            if (source == AvailableItemsListBox && control == AvailableItemsListBox)
            {
                e.Effects = DragDropEffects.None;
            }
            else if (source == AvailableItemsListBox && control == SelectedItemsListBox)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else if (source == SelectedItemsListBox && control == AvailableItemsListBox)
            {
                e.Effects = DragDropEffects.Move;
            }
            else if (source == SelectedItemsListBox && control == SelectedItemsListBox)
            {
                e.Effects = DragDropEffects.Move;
            }
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }
    }
}
