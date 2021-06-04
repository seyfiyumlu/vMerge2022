using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using alexbegh.Utility.Helpers.WeakReference;

namespace alexbegh.Utility.Helpers.ViewModel
{
    /// <summary>
    /// This class acts as a base class for ViewModels.
    /// Features:
    ///     * Manages a list of child viewmodels
    ///     * Propagates "Save" method to all of its children
    ///     * Collects exceptions happening during saving
    ///       of the children, returns them as an AggregateException
    /// </summary>
    public abstract class BaseViewModel : NotifyPropertyChangedImpl
    {
        #region Protected Constructor
        /// <summary>
        /// Constructs an instance. Must be passed the derived classes' type
        /// (for dependency propagation of NotifyPropertyChangedImpl)
        /// </summary>
        /// <param name="callerType">The type of the derived class</param>
        protected BaseViewModel(Type callerType)
            : base(callerType)
        {
            _childViewModelsDictionary = new Dictionary<string, BaseViewModel>();
            _childViewModels = new ObservableCollection<BaseViewModel>();
            _readOnlyChildViewModels = new ReadOnlyObservableCollection<BaseViewModel>(_childViewModels);
            _links = new Dictionary<string, Delegate>();
            CreatorSynchronizationContext = SynchronizationContext.Current;
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// The active synchronization context in the constructor
        /// </summary>
        protected SynchronizationContext CreatorSynchronizationContext
        {
            get;
            private set;
        }
        #endregion

        #region Private Fields
        /// <summary>
        /// Dictionary of child view models
        /// </summary>
        private Dictionary<string, BaseViewModel> _childViewModelsDictionary;

        /// <summary>
        /// Collection of child view models
        /// </summary>
        private ObservableCollection<BaseViewModel> _childViewModels;

        /// <summary>
        /// Collection of child view models in a read-only variant for exposing purposes
        /// </summary>
        private ReadOnlyObservableCollection<BaseViewModel> _readOnlyChildViewModels;

        /// <summary>
        /// List of WeakReference-linked listeners to change notification events
        /// </summary>
        Dictionary<string, Delegate> _links;
        #endregion

        #region Public Properties
        /// <summary>
        /// The child view models of this view model
        /// </summary>
        public ReadOnlyObservableCollection<BaseViewModel> ChildViewModels
        {
            get
            {
                return _readOnlyChildViewModels;
            }
        }

        /// <summary>
        /// The key parameter of this view models (the key this view model is
        /// referenced by as a child of its parent)
        /// </summary>
        public string Key
        {
            get;
            private set;
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// SaveInternal method. The derived class must serialize its contents
        /// in there.
        /// Any exceptions raised are collected and thrown at the end of the serialization
        /// process as an AggregateException.
        /// </summary>
        protected abstract void SaveInternal(object data);
        #endregion

        #region Private Methods
        /// <summary>
        /// Saves the data
        /// </summary>
        /// <param name="data">Parameter data</param>
        /// <param name="exceptions">Exception list</param>
        private void SaveHelper(object data, List<Exception> exceptions)
        {
            try
            {
                SaveInternal(data);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            foreach (var childViewModel in _childViewModels)
            {
                try
                {
                    childViewModel.SaveHelper(data, exceptions);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Saves the data of this view model; propagates to children
        /// </summary>
        /// <param name="data">Parameter</param>
        public void Save(object data)
        {
            var exceptions = new List<Exception>();
            SaveHelper(data, exceptions);
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Binds an action to a specific property.
        /// </summary>
        /// <param name="propertyName">The property to bind against</param>
        /// <param name="action">The action to be called</param>
        public void Bind(string propertyName, EventHandler<EventArgs> action)
        {
            if (!ValidatePublicPropertyNames(CallerType, propertyName))
                throw new ArgumentException("Invalid property name");

            Delegate list;
            if (!_links.TryGetValue(propertyName, out list))
            {
                _links[propertyName] =
                    WeakReferenceEventHandler.MakeWeakHandler(
                        action, 
                        rem => 
                            {
                                var handler = (EventHandler<EventArgs>)_links[propertyName];
                                handler -= rem;
                            });
            }
            else
            {
                _links[propertyName] = (EventHandler<EventArgs>)list +
                    WeakReferenceEventHandler.MakeWeakHandler(
                        action,
                        rem =>
                        {
                            var handler = (EventHandler<EventArgs>)_links[propertyName];
                            handler -= rem;
                        });
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Invoke handlers related to the specified property
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        protected void Invoke(string propertyName)
        {
            Delegate list;
            if (!_links.TryGetValue(propertyName, out list))
                return;

            list.DynamicInvoke(this, null);
        }

        /// <summary>
        /// Raises a property changed event
        /// </summary>
        /// <param name="propertyName">The properties name</param>
        protected override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);
            Invoke(propertyName);
        }

        /// <summary>
        /// Internal method, see BaseViewModelExtenstions
        /// </summary>
        /// <param name="propertyName">The property</param>
        internal void RaiseHelper(string propertyName)
        {
            RaisePropertyChanged(propertyName);
        }
        #endregion

        #region Indexer
        /// <summary>
        /// Provides easy access to existing children by key
        /// </summary>
        /// <param name="key">The child view models' key</param>
        /// <returns>The child view model</returns>
        public BaseViewModel this[string key]
        {
            get
            {
                BaseViewModel result = null;
                if( !_childViewModelsDictionary.TryGetValue(key, out result) )
                {
                    throw new ArgumentOutOfRangeException("key", "Unknown child view model key '"+key+"'");
                }
                return result;
            }
            set
            {
                BaseViewModel result = null;

                if (value.Key != null)
                    throw new InvalidOperationException("View Model with key '" + key + "' already existed.");

                if (_childViewModelsDictionary.TryGetValue(key, out result))
                {
                    if (result == value)
                        return;
                    _childViewModels.Remove(result);
                }

                value.Key = key;
                _childViewModelsDictionary[key] = value;
                _childViewModels.Add(value);
                if (result is IDisposable)
                {
                    (result as IDisposable).Dispose();
                }
                RaisePropertyChanged();
            }
        }
        #endregion
    }

    /// <summary>
    /// Extension method for calling RaisePropertyChanged more conveniently
    /// </summary>
    public static class BaseViewModelExtenstions
    {
        /// <summary>
        /// Raise property changed for a given property
        /// </summary>
        /// <typeparam name="T">The BaseViewModel subclass</typeparam>
        /// <typeparam name="T_Arg">The property type</typeparam>
        /// <param name="model">The instance</param>
        /// <param name="propAccess">The property to fire the event for</param>
        public static void RaisePropertyChanged<T, T_Arg>(this T model, System.Linq.Expressions.Expression<Func<T, T_Arg>> propAccess) where T : BaseViewModel
        {
            var expr = propAccess.Body as System.Linq.Expressions.MemberExpression;
            if (expr == null)
                throw new ArgumentException("Not a valid MemberExpression!", "propAccess");

            model.RaiseHelper(expr.Member.Name);
        }
    }
}
