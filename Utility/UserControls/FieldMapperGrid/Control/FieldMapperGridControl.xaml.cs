using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.Utility.Helpers.WPFBindings;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace alexbegh.Utility.UserControls.FieldMapperGrid.Control
{
    /// <summary>
    /// Interaction logic for FieldMapperGridControl.xaml
    /// </summary>
    public partial class FieldMapperGridControl : UserControl
    {
        #region Dependency Properties
        /// <summary>
        /// Dependency property: ItemTemplate
        /// </summary>
        public static readonly DependencyProperty ItemTemplatesProperty =
            DependencyProperty.Register("ItemTemplates",
                typeof(Dictionary<string, DataTemplate>),
                typeof(FieldMapperGridControl),
                new UIPropertyMetadata(null));

        /// <summary>
        /// Dependency property: BackgroundPath
        /// </summary>
        public static readonly DependencyProperty BackgroundPathProperty =
            DependencyProperty.Register("BackgroundPath",
                typeof(string),
                typeof(FieldMapperGridControl),
                new UIPropertyMetadata(null));

        /// <summary>
        /// Dependency property: ItemsSource
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource",
                typeof(IFieldMapperGridViewModel),
                typeof(FieldMapperGridControl),
                new UIPropertyMetadata(null));

        /// <summary>
        /// Dependency property: SelectedItemProperty
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem",
                typeof(object),
                typeof(FieldMapperGridControl),
                new UIPropertyMetadata(null));

        private void SelectedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var fmgvm = DataContext as IFieldMapperGridViewModel;
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                        InnerControl.SelectedItems.Add(item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                        InnerControl.SelectedItems.Remove(item);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                        InnerControl.SelectedItems.Remove(item);
                    foreach (var item in e.NewItems)
                        InnerControl.SelectedItems.Add(item);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    InnerControl.SelectedItems.Clear();
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The backing property for ItemTemplate
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification="Required by WPF")]
        [Bindable(true)]
        public Dictionary<string, DataTemplate> ItemTemplates
        {
            get { return (Dictionary<string, DataTemplate>)this.GetValue(ItemTemplatesProperty); }
            set { this.SetValue(ItemTemplatesProperty, value); }
        }

        /// <summary>
        /// The backing property for BackgroundPath
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required by WPF")]
        [Bindable(true)]
        public string BackgroundPath
        {
            get { return (string)this.GetValue(BackgroundPathProperty); }
            set { this.SetValue(BackgroundPathProperty, value); }
        }

        /// <summary>
        /// The backing property for ItemsSource
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required by WPF")]
        [Bindable(true)]
        public IFieldMapperGridViewModel ItemsSource
        {
            get { return (IFieldMapperGridViewModel)this.GetValue(ItemsSourceProperty); }
            set { this.SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// The backing property for ItemsSource
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required by WPF")]
        [Bindable(true)]
        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }
        #endregion

        /// <summary>
        /// Constructs the control
        /// </summary>  
        public FieldMapperGridControl()
        {
            InitializeComponent();
            ItemTemplates = new Dictionary<string, DataTemplate>();
        }

        private DataGrid _innerControl;
        internal DataGrid InnerControl
        {
            get { return _innerControl; }
            set { _innerControl = value; }
        }
    }
}
