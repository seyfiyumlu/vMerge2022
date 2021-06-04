using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace alexbegh.Utility.UserControls.FieldMapperGrid
{
    /// <summary>
    /// This class serves as the DataContext of a ListView to bind against.
    /// XAML example:
    ///    &lt;ListView ItemsSource="{Binding Path=Elements}" View="{Binding Path=., Converter={StaticResource FieldMapperGridViewConverter}}" /&gt;
    /// </summary>
    /// <typeparam name="T_Item">The type of item to expose</typeparam>
    public sealed class FieldMapperGridViewModel<T_Item> : NotifyPropertyChangedImpl, IFieldMapperGridViewModel
    {
        #region Private Properties
        /// <summary>
        /// The source collection to wrap
        /// </summary>
        private ObservableCollection<T_Item> Source { get; set; }

        /// <summary>
        /// The data accessor.
        /// </summary>
        private IPropertyAccessor<T_Item> DataAccessor { get; set; }

        /// <summary>
        /// Backing property for the publicly visible InternalSelectedElements
        /// </summary>
        private ObservableCollection<object> InternalSelectedElements { get; set; }
        #endregion

        #region Public Properties
        /// <summary>
        /// The contained elements, wrapped in a FieldExposer instance
        /// </summary>
        public ObservableCollection<FieldExposer<T_Item>> Elements
        { 
            get; 
            private set; 
        }

        /// <summary>
        /// The selected elements
        /// </summary>
        ObservableCollection<object> IFieldMapperGridViewModel.InternalSelectedElements
        {
            get { return InternalSelectedElements; }
        }

        object IFieldMapperGridViewModel.Elements
        {
            get { return this.Elements; }
        }

        /// <summary>
        /// Meta data
        /// </summary>
        public object MetaData
        {
            get;
            set;
        }

        /// <summary>
        /// The contained elements, wrapped in a FieldExposer instance
        /// </summary>
        public ObservableCollection<T_Item> OriginalElements
        {
            get;
            private set;
        }

        /// <summary>
        /// The selected elements
        /// </summary>
        public ObservableCollection<T_Item> SelectedElements
        {
            get;
            private set;
        }

        /// <summary>
        /// Available Columns
        /// </summary>
        public ObservableCollection<FieldMapperGridColumn> Columns
        {
            get;
            private set;
        }

        private event EventHandler _columnSettingsChanged;
        /// <summary>
        /// This event is raised when column settings have been changed
        /// </summary>
        public event EventHandler ColumnSettingsChanged
        {
            add { _columnSettingsChanged += value; }
            remove { _columnSettingsChanged -= value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="dataAccessor">The data accessor for the T_Item type.</param>
        /// <param name="source">The source collection.</param>
        /// <param name="data">The initial data (usually an ObservableCollection of T_Item)</param>
        public FieldMapperGridViewModel(IPropertyAccessor<T_Item> dataAccessor, ObservableCollection<T_Item> source, object data = null)
            : base(typeof(FieldMapperGridViewModel<T_Item>))
        {
            // Initialize
            DataAccessor = dataAccessor;
            Source = source;
            OriginalElements = source;
            SelectedElements = new ObservableCollection<T_Item>();
            InternalSelectedElements = new ObservableCollection<object>();

            Elements = new ObservableCollection<FieldExposer<T_Item>>();

            if (data == null)
                Columns = new ObservableCollection<FieldMapperGridColumn>();
            else
                Columns = (data as ObservableCollection<FieldMapperGridColumn>);

            foreach (var col in Columns)
                col.Parent = this;
            Columns.CollectionChanged += Columns_CollectionChanged;

            SetContent(source);

            // Catch if the source collection is modified
            Source.CollectionChanged += Source_CollectionChanged;

            // Catch if the selected elements collection is modified
            SelectedElements.CollectionChanged += SelectedElements_CollectionChanged;
            InternalSelectedElements.CollectionChanged += InternalSelectedElements_CollectionChanged;
        }

        void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var col in e.OldItems.OfType<FieldMapperGridColumn>())
                {
                    col.Parent = null;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var col in e.NewItems.OfType<FieldMapperGridColumn>())
                {
                    col.Parent = this;
                }
            }
        }

        void SelectedElements_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SuppressNotificationsCounter > 0)
                return;

            try
            {
                SuppressNotifications();
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                        if (e.OldItems != null)
                        {
                            foreach (var item in e.OldItems)
                            {
                                var mappedItem = Elements.Where(element => element.InnerItem.Equals((T_Item)item)).FirstOrDefault();
                                if (mappedItem == null)
                                    throw new ArgumentOutOfRangeException("The removed element is not part of the list");
                                InternalSelectedElements.Remove(mappedItem);
                            }
                        }
                        if (e.NewItems != null)
                        {
                            foreach (var item in e.NewItems)
                            {
                                var mappedItem = Elements.Where(element => element.InnerItem.Equals((T_Item)item)).FirstOrDefault();
                                if (mappedItem == null)
                                    throw new ArgumentOutOfRangeException("The added element is not part of the list");
                                InternalSelectedElements.Add(mappedItem);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        InternalSelectedElements.Clear();
                        break;
                }
            }
            finally
            {
                AllowNotifications();
            }
        }

        private void InternalSelectedElements_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SuppressNotificationsCounter > 0)
                return;

            try
            {
                SuppressNotifications();
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                        if (e.OldItems != null)
                        {
                            foreach (var item in e.OldItems.Cast<FieldExposer<T_Item>>())
                            {
                                if (SelectedElements.Contains(item.InnerItem))
                                    SelectedElements.Remove(item.InnerItem);
                            }
                        }
                        if (e.NewItems != null)
                        {
                            foreach (var item in e.NewItems.Cast<FieldExposer<T_Item>>())
                            {
                                if (!SelectedElements.Contains(item.InnerItem))
                                    SelectedElements.Add(item.InnerItem);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        SelectedElements.Clear();
                        break;
                }
            }
            finally
            {
                AllowNotifications();
            }
        }

        /// <summary>
        /// Sets the content altogether
        /// </summary>
        /// <param name="source">The data to set</param>
        public void SetContent(ObservableCollection<T_Item> source)
        {
            Elements.Clear();
            // Wrap all elements in Source, build the list of available columns
            Source = source;
            foreach (var item in source)
            {
                Elements.Add(new FieldExposer<T_Item>(DataAccessor, item));
                foreach (var field in DataAccessor.AllFieldsOf(item))
                {
                    var matchingColumn = Columns.FirstOrDefault(col => col.Column == field);
                    if (matchingColumn != null)
                    {
                        matchingColumn.Type = DataAccessor.GetType(item, field);
                        continue;
                    }

                    Columns.Add(new FieldMapperGridColumn() { Parent = this, Header = field, Column = field, Width = 100, Visible = false, Type = DataAccessor.GetType(item, field) });
                }
            }
            OriginalElements = source;
            RaisePropertyChanged("Columns");
        }

        /// <summary>
        /// Sets the columns of the grid
        /// </summary>
        /// <param name="columns">Columns to set</param>
        public void SetColumns(ObservableCollection<FieldMapperGridColumn> columns)
        {
            Columns.CollectionChanged -= Columns_CollectionChanged;
            foreach (var col in columns)
                col.Parent = this;
            Columns = columns;
            Columns.CollectionChanged += Columns_CollectionChanged;
        }
        #endregion

        #region Public Operations
        /// <summary>
        /// This method returns the internal data to serialize its status
        /// </summary>
        /// <returns>An object containing the internal state</returns>
        public object Save()
        {
            return Columns;
        }

        /// <summary>
        /// Returns the value of a given item and column
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="columnName">The column</param>
        /// <returns>The value</returns>
        public object GetValueOf(object item, string columnName)
        {
            return ((FieldExposer<T_Item>)item)[columnName];
        }
        #endregion

        #region Internal Operations
        /// <summary>
        /// Raises the "column settings changed" event
        /// </summary>
        public void RaiseColumnSettingsChanged()
        {
            if (_columnSettingsChanged != null)
                _columnSettingsChanged(this, new EventArgs());
        }
        #endregion

        #region Private Event Handlers
        /// <summary>
        /// Act when the source collection has been changed.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The kind of modification</param>
        private void Source_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        Elements.Add(new FieldExposer<T_Item>(DataAccessor, (T_Item)item));
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems.Count == 1)
                    {
                        T_Item item = (T_Item)e.OldItems[0];
                        var found = Elements.Where(element => element.InnerItem.Equals(item)).FirstOrDefault();
                        if (found == null)
                            throw new InvalidOperationException("Unexpected behavior: element not in list!");
                        Elements.Remove(found);
                        var selected = SelectedElements.Where(element => element.Equals(item)).FirstOrDefault();
                        if (selected != null)
                            SelectedElements.Remove(selected);
                    }
                    else
                        throw new InvalidOperationException("Cannot remove more than one item at once!");
                    break;
                default:
                    Elements = new ObservableCollection<FieldExposer<T_Item>>();
                    foreach (var item in (ObservableCollection<T_Item>)sender)
                        Elements.Add(new FieldExposer<T_Item>(DataAccessor, (T_Item)item));
                    RaisePropertyChanged("Elements");
                    break;
            }
        }
        #endregion

    }
}
