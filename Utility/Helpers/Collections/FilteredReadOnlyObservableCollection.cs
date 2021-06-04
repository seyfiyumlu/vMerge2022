using alexbegh.Utility.Helpers.WeakReference;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.Collections
{
    /// <summary>
    /// Provides a read only observable collection able to apply a filter
    /// </summary>
    /// <typeparam name="Type">The wrapped type</typeparam>
    public class FilteredReadOnlyObservableCollection<Type> : IList<Type>, IEnumerable<Type>, INotifyCollectionChanged where Type: class
    {
        #region Public Properties
        private ObservableCollection<Type> _source;

        /// <summary>
        /// The source collection
        /// </summary>
        public ObservableCollection<Type> Source
        {
            get { return _source; }
            private set { _source = value; }
        }

        private PropertyChangedEventHandler _handler;

        private bool _ignoreSourceChangeEvents;

        private Func<Type, bool> _filter;
        /// <summary>
        /// The Filter delegate
        /// </summary>
        public Func<Type, bool> Filter
        {
            get
            {
                return _filter;
            }
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    Filtered.Clear();
                    foreach (var item in Source)
                    {
                        if (item is INotifyPropertyChanged)
                            ((INotifyPropertyChanged)item).PropertyChanged += _handler;
                        if (Filter(item))
                            Filtered.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Swaps two items in the filtered list and applies the swap onto the original list
        /// </summary>
        /// <param name="item1">First item</param>
        /// <param name="item2">Second item</param>
        public void Swap(Type item1, Type item2)
        {
            _ignoreSourceChangeEvents = true;
            try
            {
                Swap(Source, item1, item2);
                Swap(Filtered, item1, item2);
            }
            finally
            {
                _ignoreSourceChangeEvents = false;
            }
        }

        private static void Swap(ObservableCollection<Type> list, Type item1, Type item2)
        {
            int originalIndex1 = list.IndexOf(item1);
            int originalIndex2 = list.IndexOf(item2);

            if (originalIndex1 == -1)
                throw new ArgumentException("Item not contained in source collection", "item1");
            if (originalIndex2 == -1)
                throw new ArgumentException("Item not contained in source collection", "item2");

            if (originalIndex2 < originalIndex1)
            {
                originalIndex1 = Interlocked.Exchange(ref originalIndex2, originalIndex1);
                item1 = Interlocked.Exchange<Type>(ref item2, item1);
            }

            if ((originalIndex1 + 1) == originalIndex2)
            {
                list.Insert(originalIndex1, item2);
                list.RemoveAt(originalIndex1 + 2);
            }
            else
            {
                list.Insert(originalIndex1, item2);
                list.RemoveAt(originalIndex1 + 1);
                list.Insert(originalIndex2, item1);
                list.RemoveAt(originalIndex2 + 1);
            }
        }


        private void Handler(object sender, EventArgs args)
        {
            var item = (Type)sender;
            bool filterApplies = Filter(item);
            if (filterApplies)
            {
                if (!Filtered.Contains(item))
                {
                    var previous = Filtered.LastOrDefault();
                    if (previous != null)
                    {
                        _ignoreSourceChangeEvents = true;
                        try
                        {
                            int idxInsertAfter = Source.IndexOf(previous);
                            Source.Remove(item);
                            if (idxInsertAfter < Source.Count)
                                Source.Insert(idxInsertAfter, item);
                            else
                                Source.Add(item);
                        }
                        finally
                        {
                            _ignoreSourceChangeEvents = false;
                        }
                    }
                    Filtered.Add(item);
                }
                return;
            }
            else
            {
                if (Filtered.Contains(item))
                    Filtered.Remove(item);
                return;
            }
        }
        
        /// <summary>
        /// Collection changed event
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        #region Private Properties
        private ObservableCollection<Type> _filtered;

        /// <summary>
        /// The filtered collection
        /// </summary>
        private ObservableCollection<Type> Filtered
        {
            get { return _filtered; }
            set { _filtered = value; }
        }
        #endregion

        #region Private Event Handlers
        /// <summary>
        /// Source collection changed
        /// </summary>
        /// <param name="sender">Originating source</param>
        /// <param name="e">EventArgs</param>
        private void source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_ignoreSourceChangeEvents)
                return;

            var newItems = (e.NewItems == null) ? null : e.NewItems.Cast<Type>();
            var oldItems = (e.OldItems == null) ? null : e.OldItems.Cast<Type>();

            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in newItems.Where(Filter))
                    {
                        if (item is INotifyPropertyChanged)
                            ((INotifyPropertyChanged)item).PropertyChanged += _handler;
                        Type predecessor = null;
                        for (int idx = Source.IndexOf(item); predecessor == null && idx >= 0; --idx)
                        {
                            var current = Source[idx];
                            if (Filter(current) && Filtered.Contains(current))
                            {
                                predecessor = current;
                            }
                        }
                        if (predecessor == null)
                            Filtered.Insert(0, item);
                        else
                        {
                            var insertIdx = Filtered.IndexOf(predecessor) + 1;
                            if (insertIdx == Filtered.Count)
                                Filtered.Add(item);
                            else
                                Filtered.Insert(insertIdx, item);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in oldItems.Where(Filter))
                    {
                        if (item is INotifyPropertyChanged)
                            ((INotifyPropertyChanged)item).PropertyChanged -= _handler;
                        Filtered.Remove(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in Filtered)
                    {
                        if (item is INotifyPropertyChanged)
                            ((INotifyPropertyChanged)item).PropertyChanged -= _handler;
                    }
                    Filtered.Clear();
                    foreach (var item in Source.Where(Filter))
                    {
                        if (item is INotifyPropertyChanged)
                            ((INotifyPropertyChanged)item).PropertyChanged += _handler;
                        Filtered.Add(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var item in Filtered)
                    {
                        if (item is INotifyPropertyChanged)
                            ((INotifyPropertyChanged)item).PropertyChanged -= _handler;
                    }
                    Filtered.Clear();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Filtered collection changed
        /// </summary>
        /// <param name="sender">Originating source</param>
        /// <param name="e">EventArgs</param>
        private void filtered_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, e);
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="source">The source collection</param>
        /// <param name="filter">The filter delegate</param>
        public FilteredReadOnlyObservableCollection(ObservableCollection<Type> source, Func<Type, bool> filter)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (filter == null)
                throw new ArgumentNullException("filter");

            _ignoreSourceChangeEvents = false;
            Source = source;
            _filter = filter;
            _handler = WeakReferenceEventHandler.MakeWeakPropertyChangedHandler(Handler, (item, rem) => { item.PropertyChanged -= rem; });
            Filtered = new ObservableCollection<Type>(source.Where(filter));
            source.CollectionChanged += source_CollectionChanged;
            Filtered.CollectionChanged += filtered_CollectionChanged;

            foreach (var item in Source)
            {
                if (item is INotifyPropertyChanged)
                    ((INotifyPropertyChanged)item).PropertyChanged += _handler;
            }
        }
        #endregion

        #region Interface methods
        /// <summary>
        /// IndexOf
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(Type item)
        {
            return Filtered.IndexOf(item);
        }

        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, Type item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// RemoveAt
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Type this[int index]
        {
            get
            {
                return Filtered[index];
            }
            set
            {
                Filtered[index] = value;
            }
        }

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="item"></param>
        public void Add(Type item)
        {
            Source.Add(item);
        }

        /// <summary>
        /// Clear
        /// </summary>
        public void Clear()
        {
            Source.Clear();
        }

        /// <summary>
        /// Contains
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(Type item)
        {
            return Filtered.Contains(item);
        }

        /// <summary>
        /// CopyTo
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(Type[] array, int arrayIndex)
        {
            Filtered.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Count
        /// </summary>
        public int Count
        {
            get { return Filtered.Count; }
        }

        /// <summary>
        /// IsReadOnly
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(Type item)
        {
            return Source.Remove(item);
        }

        /// <summary>
        /// GetEnumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Type> GetEnumerator()
        {
            return Filtered.GetEnumerator();
        }

        /// <summary>
        /// GetEnumerator
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Filtered.GetEnumerator();
        }
        #endregion
    }
}
