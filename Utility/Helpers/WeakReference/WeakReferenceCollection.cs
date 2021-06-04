using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.WeakReference
{
    /// <summary>
    /// Weakly referenced list of items
    /// </summary>
    /// <typeparam name="T">Type of item to reference</typeparam>
    public class WeakReferenceList<T> : IEnumerable<T> where T: class
    {
        private List<WeakReference<T>> _data;

        /// <summary>
        /// Constructs an instance
        /// </summary>
        public WeakReferenceList()
        {
            _data = new List<WeakReference<T>>();
        }

        private class WeakReferenceListIterator<Ti> : IEnumerator<Ti> where Ti: class
        {
            private IEnumerator<WeakReference<Ti>> _listEnumerator;

            public WeakReferenceListIterator(IEnumerator<WeakReference<Ti>> listEnumerator)
            {
                _listEnumerator = listEnumerator;
            }

            public Ti Current
            {
                get
                {
                    Ti target;
                    while (!_listEnumerator.Current.TryGetTarget(out target))
                    {
                        if (false == MoveNext())
                            return null;
                    }
                    return target;
                }
            }

            public void Dispose()
            {
                _listEnumerator.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                bool okay = _listEnumerator.MoveNext();
                while (okay) 
                {
                    Ti target;
                    if (_listEnumerator.Current.TryGetTarget(out target))
                        return true;
                    okay = _listEnumerator.MoveNext();
                }
                return false;
            }

            public void Reset()
            {
                _listEnumerator.Reset();
            }
        }

        /// <summary>
        /// Return an enumerator to the alive elements
        /// </summary>
        /// <returns>An enumerator</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new WeakReferenceListIterator<T>(_data.GetEnumerator());
        }

        /// <summary>
        /// Return an enumerator to the alive elements
        /// </summary>
        /// <returns>An enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an item
        /// </summary>
        /// <param name="item">The item to add</param>
        public void Add(T item)
        {
            _data.Add(new WeakReference<T>(item));
        }

        /// <summary>
        /// Clears all items
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }

        /// <summary>
        /// Removes stale references
        /// </summary>
        /// <returns>true if anything has been removed</returns>
        public bool Compact()
        {
            for (int i = 0; i < _data.Count; ++i)
            {
                T target;
                if (!_data[i].TryGetTarget(out target))
                {
                    _data[i] = null;
                }
            }
            return _data.RemoveAll(item => item == null) != 0;
        }

        /// <summary>
        /// Removes stale references
        /// </summary>
        /// <returns>true if anything has been removed</returns>
        public List<T> CompactAndReturn()
        {
            var result = new List<T>();
            for (int i = 0; i < _data.Count; ++i)
            {
                T target;
                if (!_data[i].TryGetTarget(out target))
                {
                    _data[i] = null;
                }
                else
                    result.Add(target);
            }
            _data.RemoveAll(item => item == null);
            return result;
        }
    }
}
