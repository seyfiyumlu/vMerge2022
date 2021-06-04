using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.WeakReference
{
    class WeakReferenceWithMetaData<T, U> where T : class
    {
        private WeakReference<T> _t;
        private U _u;

        public WeakReferenceWithMetaData(T t, U u)
        {
            _t = new WeakReference<T>(t);
            _u = u;
        }

        public U Data
        {
            get { return _u; }
        }

        public bool TryGetTarget(out T target)
        {
            return _t.TryGetTarget(out target);
        }
    }

    /// <summary>
    /// Weakly referenced list of items
    /// </summary>
    /// <typeparam name="T">Type of item to reference</typeparam>
    /// <typeparam name="U">Type of metadata to add</typeparam>
    public class WeakReferenceWithMetaDataList<T, U> where T: class
    {
        private List<WeakReferenceWithMetaData<T, U>> _data;

        /// <summary>
        /// Constructs an instance
        /// </summary>
        public WeakReferenceWithMetaDataList()
        {
            _data = new List<WeakReferenceWithMetaData<T,U>>();
        }

        /// <summary>
        /// Adds an item
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="data">The metadata to add</param>
        public void Add(T item, U data)
        {
            _data.Add(new WeakReferenceWithMetaData<T, U>(item, data));
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
        public List<Tuple<T, U>> CompactAndReturn()
        {
            var result = new List<Tuple<T, U>>();
            for (int i = 0; i < _data.Count; ++i)
            {
                T target;
                if (!_data[i].TryGetTarget(out target))
                {
                    _data[i] = null;
                }
                else
                    result.Add(new Tuple<T,U>(target, _data[i].Data));
            }
            _data.RemoveAll(item => item == null);
            return result;
        }
    }
}
