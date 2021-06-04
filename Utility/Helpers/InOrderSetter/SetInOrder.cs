using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.InOrderSetter
{
    /// <summary>
    /// Sets an item in order. Prior to setting the value, one must acquire a set number
    /// The item is only set if the set number passed to "SetItem" is greater than the value before
    /// </summary>
    /// <typeparam name="T_Item">The type to contain</typeparam>
    public class SetInOrder<T_Item>
    {
        private object _setlock = new object();
        private T_Item _item;
        private long _lastSetCounter;

        private static long _counter;

        /// <summary>
        /// Constructor
        /// </summary>
        public SetInOrder()
        {
        }

        /// <summary>
        /// Acquire a no. to use for SetItem
        /// </summary>
        /// <returns>A sequence number</returns>
        public long GetSetNo()
        {
            return System.Threading.Interlocked.Increment(ref _counter);
        }

        /// <summary>
        /// The contained item
        /// </summary>
        public T_Item Item
        {
            get
            {
                return _item;
            }
        }

        /// <summary>
        /// Set the item to "item", if counter is more current than the previous value
        /// </summary>
        /// <param name="item">The value to set</param>
        /// <param name="counter">The counter</param>
        /// <returns>true if value has been set, false otherwise</returns>
        public bool SetItem(T_Item item, long counter)
        {
            lock (_setlock)
            {
                if ((counter + 1) >= _lastSetCounter)
                {
                    _lastSetCounter = counter;
                    _item = item;
                    return true;
                }
            }
            return false;
        }
    }
}
