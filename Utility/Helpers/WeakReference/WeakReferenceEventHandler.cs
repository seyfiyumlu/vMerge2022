using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.WeakReference
{
    /// <summary>
    /// Creates weak referenced event handlers
    /// </summary>
    public static class WeakReferenceEventHandler
    {
        /// <summary>
        /// MakeWeakHandler
        /// </summary>
        /// <param name="action"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public static EventHandler<EventArgs> MakeWeakHandler(EventHandler<EventArgs> action, Action<EventHandler<EventArgs>> remove)
        {
            var reference = new System.WeakReference(action.Target);
            var method = action.Method;
            EventHandler<EventArgs> handler = null;
            handler = delegate(object sender, EventArgs e)
            {
                var target = reference.Target;
                if (target != null)
                {
                    method.Invoke(target, new object[] { sender, e });
                }
                else
                {
                    remove(handler);
                }
            };
            return handler;
        }

        /// <summary>
        /// MakeWeakPropertyChangedHandler
        /// </summary>
        /// <param name="action"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public static PropertyChangedEventHandler MakeWeakPropertyChangedHandler(PropertyChangedEventHandler action, Action<INotifyPropertyChanged, PropertyChangedEventHandler> remove)
        {
            var reference = new System.WeakReference(action.Target);
            var method = action.Method;
            PropertyChangedEventHandler handler = null;
            handler = delegate(object sender, PropertyChangedEventArgs e)
            {
                var target = reference.Target;
                if (target != null)
                {
                    method.Invoke(target, new object[] { sender, e });
                }
                else
                {
                    remove((INotifyPropertyChanged)sender, handler);
                }
            };
            return handler;
        }

        /// <summary>
        /// MakeWeakCollectionChangeHandler
        /// </summary>
        /// <param name="action"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public static NotifyCollectionChangedEventHandler MakeWeakCollectionChangedHandler(NotifyCollectionChangedEventHandler action, Action<INotifyCollectionChanged, NotifyCollectionChangedEventHandler> remove)
        {
            var reference = new System.WeakReference(action.Target);
            var method = action.Method;
            NotifyCollectionChangedEventHandler handler = null;
            handler = delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                var target = reference.Target;
                if (target != null)
                {
                    method.Invoke(target, new object[] { sender, e });
                }
                else
                {
                    remove((INotifyCollectionChanged)sender, handler);
                }
            };
            return handler;
        }

    }
}
