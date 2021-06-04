using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using alexbegh.Utility.Helpers.WPFBindings;
using System.Windows.Markup;
using System.Windows.Media;
using alexbegh.Utility.Helpers.Converters;
using alexbegh.Utility.Helpers.WeakReference;
using System.Threading;
using System.Collections.Specialized;
using alexbegh.Utility.Managers.Background;
using alexbegh.Utility.SerializationHelpers;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace alexbegh.Utility.UserControls.FieldMapperGrid.Control
{
    /// <summary>
    /// Internally used
    /// </summary>
    [RegisterForSerialization]
    [XmlInclude(typeof(ListSortDirection))]
    [XmlInclude(typeof(int))]
    [XmlInclude(typeof(Visibility))]
    public struct PrivateFieldData
    {
        /// <summary>
        /// Internally used
        /// </summary>
        public int DisplayIndex;
        /// <summary>
        /// Internally used
        /// </summary>
        public ListSortDirection? SortDirection;
        /// <summary>
        /// Internally used
        /// </summary>
        public Visibility Visibility;
    }

    /// <summary>
    /// This converter transmogrifies the WPF binding
    /// </summary>
    public class FieldMapperGridViewConverter : IValueConverter
    {
        /// <summary>
        /// The resource dictionary containing the DataTemplate for the list of items
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required by WPF")]
        public ResourceDictionary ResourceDictionary { get; set; }

        //static object boundTo = null;
        /// <summary>
        /// WPF converter to convert a FieldMapperGridViewModel DataContext to a ListView
        /// </summary>
        /// <param name="value">The source value (FieldMapperGridViewModel)</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>A list of MenuItems</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fmgvm = value as IFieldMapperGridViewModel;
            if (fmgvm != null)
            {
                var mainGrid = new Grid();
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
                mainGrid.RowDefinitions.Add(new RowDefinition());

                var dataGrid = new DataGrid();
                VirtualizingStackPanel.SetScrollUnit(dataGrid, ScrollUnit.Pixel);
                dataGrid.AutoGenerateColumns = false;
                dataGrid.EnableRowVirtualization = true;
                VirtualizingStackPanel.SetVirtualizationMode(dataGrid, VirtualizationMode.Standard);
                VirtualizingStackPanel.SetIsVirtualizing(dataGrid, true);
                dataGrid.CanUserReorderColumns = false;
                dataGrid.CanUserAddRows = false;
                dataGrid.CanUserDeleteRows = false;
                dataGrid.IsReadOnly = true;
                dataGrid.DataContext = fmgvm;
                dataGrid.VerticalContentAlignment = VerticalAlignment.Top;
                dataGrid.GridLinesVisibility = DataGridGridLinesVisibility.All;
                dataGrid.HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(224, 224, 255));
                dataGrid.VerticalGridLinesBrush = new SolidColorBrush(Color.FromRgb(224, 224, 255));
                //RoutedEventHandler rowHandler = (o,a) =>
                //    {
                //        var row = a.Source as DataGridRow;
                //        foreach (var col in dataGrid.Columns)
                //        {
                //            col.GetCellContent(row).Tag = fmgvm.GetValueOf(row.DataContext, col.SortMemberPath);
                //        }
                //        Console.WriteLine(a.ToString());
                //    };
//                EventHandler<DataGridRowEventArgs> func = (o,a) =>
//                    {
//                        BindingOperations.SetBinding(a.Row, DataGridRow.FontWeightProperty, new Binding("FontWeight"));
////                        a.Row.Loaded += rowHandler;
//                    };
                //dataGrid.LoadingRow += func;
                //dataGrid.Unloaded += (o, a) => { dataGrid.LoadingRow -= func; };

                PropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));

                BindingExpressionBase itemsSourceBindingExpression = BindingOperations.SetBinding(
                    dataGrid, DataGrid.ItemsSourceProperty,
                    new Binding("Elements"));

                dataGrid.Loaded += (o, a) =>
                    {
                        var host = DependencyObjectHelper.FindAncestor<FieldMapperGridControl>(dataGrid);

                        host.InnerControl = dataGrid;
                        fmgvm.MetaData = host;

                        BindingOperations.SetBinding(dataGrid, DataGrid.SelectedItemProperty,
                            new Binding("SelectedItem") { Source = host, Mode = BindingMode.TwoWay, Converter = new StripFieldExposerConverter(fmgvm) });

                        RebuildDataGridColumns(fmgvm, dataGrid, pd, host, true);

                        foreach (var item in fmgvm.InternalSelectedElements)
                            dataGrid.SelectedItems.Add(item);
                        dataGrid.SelectionChanged += SelectionChanged;
                    };

                ((INotifyPropertyChanged)fmgvm).PropertyChanged += (o, a) =>
                    {
                        if (fmgvm.MetaData == null)
                            return;

                        if (a.PropertyName == "Columns")
                        {
                            var host = (FieldMapperGridControl)fmgvm.MetaData;
                            RebuildDataGridColumns(fmgvm, dataGrid, pd, host);
                        }
                    };
                fmgvm.Columns.CollectionChanged += (o, a) =>
                    {
                        if (fmgvm.MetaData == null)
                            return;

                        var host = (FieldMapperGridControl)fmgvm.MetaData;
                        RebuildDataGridColumns(fmgvm, dataGrid, pd, host);
                    };

                mainGrid.Children.Add(dataGrid);
                Grid.SetRow(dataGrid, 0);
                Grid.SetColumn(dataGrid, 0);
                return mainGrid;
            }
            return Binding.DoNothing;
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = (sender as DataGrid);
            var fmgvm = grid.DataContext as IFieldMapperGridViewModel;

            foreach (var removed in e.RemovedItems)
            {
                if (fmgvm.InternalSelectedElements.Contains(removed))
                    fmgvm.InternalSelectedElements.Remove(removed);
            }
            foreach (var added in e.AddedItems)
            {
                if (!fmgvm.InternalSelectedElements.Contains(added))
                    fmgvm.InternalSelectedElements.Add(added);
            }
        }

        private void RebuildDataGridColumns(IFieldMapperGridViewModel fmgvm, DataGrid dataGrid, PropertyDescriptor pd, FieldMapperGridControl host, bool updateFromInternalData = false)
        {
            int idx = 0;
            dataGrid.Columns.Clear();
            foreach (var column in fmgvm.Columns)
            {
                DataGridColumn dataGridColumn = null;
                if (host.ItemTemplates.ContainsKey(column.Column))
                {
                    dataGridColumn = new DataGridTemplateColumn() { CellTemplate = host.ItemTemplates[column.Column] };
                    dataGridColumn.SortMemberPath = column.Column;
                }
                else if (host.ItemTemplates.ContainsKey(String.Empty))
                {
                    dataGridColumn = new DataGridTemplateColumn() { CellTemplate = host.ItemTemplates[String.Empty] };
                    dataGridColumn.SortMemberPath = column.Column;
                }
                else if (host.ItemTemplates.ContainsKey("Type:"+column.Type.FullName))
                {
                    dataGridColumn = new CustomBoundColumn() { ContentTemplate = host.ItemTemplates["Type:" + column.Type.FullName], Binding = new Binding("Item[" + column.Column + "]") };
                    dataGridColumn.SortMemberPath = column.Column;
                }
                else
                {
                    dataGridColumn = new DataGridTextColumn() { Binding = new Binding("Item[" + column.Column + "]") };
                    if (column.Type == typeof(DateTime))
                    {
                        (dataGridColumn as DataGridTextColumn).Binding.StringFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;
                    }
                    dataGridColumn.SortMemberPath = column.Column;
                }

                dataGridColumn.Header = column.Header;
                dataGridColumn.Visibility = column.Visible ? Visibility.Visible : Visibility.Collapsed;
                dataGridColumn.Width = new DataGridLength(column.Width);

                pd.AddValueChanged(dataGridColumn, (ox, ax) =>
                {
                    column.SuppressNotifications();
                    column.Width = dataGridColumn.Width.IsAuto ? dataGridColumn.Width.DisplayValue : dataGridColumn.Width.Value;
                    column.AllowNotifications();
                });
                column.PropertyChanged += (ox, ax) =>
                {
                    if (ax.PropertyName == "Visible")
                    {
                        if (column.Visible == false)
                            BackgroundTaskManager.DelayedPostIfPossible(() => { RebuildDataGridColumns(fmgvm, dataGrid, pd, host, false); return true; });
                    }
                    if (ax.PropertyName == "Width")
                    {
                        dataGridColumn.Width = new DataGridLength(column.Width);
                    }
                };
                BindingOperations.SetBinding(dataGridColumn, DataGridTemplateColumn.WidthProperty, new Binding() { Source = column.Width });
                dataGrid.Columns.Add(dataGridColumn);
                ++idx;
            }

            dataGrid.ColumnDisplayIndexChanged += (o, a) => OnSettingsChanged(dataGrid, fmgvm);
            dataGrid.ColumnReordered += (o, a) => OnSettingsChanged(dataGrid, fmgvm);
            dataGrid.Sorting += (o, a) => OnSettingsChanged(dataGrid, fmgvm);
            dataGrid.Items.SortDescriptions.Clear();

            foreach (var column in fmgvm.Columns)
            {
                var dataGridColumn = dataGrid.Columns.Where(item => item.SortMemberPath == column.Header).FirstOrDefault();
                if (dataGridColumn == null)
                    continue;

                var fromInternalData = column.InternalProperties as PrivateFieldData?;
                if (fromInternalData != null)
                {
                    try
                    {
                        dataGridColumn.SortDirection = fromInternalData.Value.SortDirection;
                        if (dataGridColumn.SortDirection.HasValue)
                        {
                            dataGrid.Items.SortDescriptions.Add(new SortDescription(dataGridColumn.SortMemberPath, dataGridColumn.SortDirection.Value));
                        }
                        if (updateFromInternalData)
                            dataGridColumn.Visibility = fromInternalData.Value.Visibility;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private bool _updatePending = false;
        private void OnSettingsChanged(DataGrid dataGrid, IFieldMapperGridViewModel fmgvm)
        {
            if (_updatePending)
                return;
            _updatePending = true;
            BackgroundTaskManager.DelayedPostIfPossible(
                () =>
                {
                    _updatePending = false;
                    foreach (var column in dataGrid.Columns)
                    {
                        var matchingViewModelColumn = fmgvm.Columns.Where(col => col.Header == column.SortMemberPath).FirstOrDefault();
                        if (matchingViewModelColumn != null)
                        {
                            var pfd = new PrivateFieldData();
                            pfd.DisplayIndex = column.DisplayIndex;
                            pfd.SortDirection = column.SortDirection;
                            pfd.Visibility = column.Visibility;
                            matchingViewModelColumn.InternalProperties = pfd;
                        }
                    }
                    fmgvm.RaiseColumnSettingsChanged();
                    return true;
                });
        }

        /// <summary>
        /// Performs the backwards conversion; not implemented
        /// </summary>
        /// <param name="value">Not used</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>nothing; throws exception</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
