using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.NotifyPropertyChanged
{
    /// <summary>
    /// <para>This class helps implementing the INotifyPropertyChanged interface.</para>
    /// <para>Features:</para>
    /// <list type="bullet">
    /// <item>Can be fed with dependent properties; when one property changes, the dependent properties' 
    ///       fire also (<see cref="AddDependency"/>, <see cref="AddDependencyGroup"/></item>
    /// <item>Automatically checks for changed values in a generic Set method (<see cref="O:Set{T}" />)</item>
    /// <item>Provides a <see cref="RaisePropertyChanged"/> method which considers the dependent properties
    ///       and derives the caller properties' name automatically</item>
    /// </list>
    /// </summary>
    public class NotifyPropertyChangedImpl : INotifyPropertyChanged
    {
        #region Static Constructor
        /// <summary>
        /// Initializes the dependent property dictionary
        /// </summary>
        static NotifyPropertyChangedImpl()
        {
            DependentProperties = new Dictionary<Type, Dictionary<string, List<string>>>();
        }
        #endregion

        #region Protected Constructor
        /// <summary>
        /// Constructs the object; remembers the callers' type for looking up dependent properties
        /// efficiently
        /// </summary>
        /// <param name="callerType">The callers (i.e. derived class) type</param>
        protected NotifyPropertyChangedImpl(Type callerType)
        {
            if( DependentProperties.ContainsKey(callerType) )
                CallerType = callerType;
        }
        #endregion

        #region Private/Protected Properties
        /// <summary>
        /// The callers type
        /// </summary>
        protected Type CallerType { get; set; }

        /// <summary>
        /// The dependent property dictionary (type -> propertyName -> dependent properties)
        /// </summary>
        private static Dictionary<Type, Dictionary<string, List<string>>> DependentProperties
        {
            get;
            set;
        }

        /// <summary>
        /// If >0, INotifyPropertyChanged events are being suppressed
        /// </summary>
        protected int SuppressNotificationsCounter { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Suppresses PropertyChanged events.
        /// </summary>
        /// <returns>The new suppression counter</returns>
        public int SuppressNotifications()
        {
            return ++SuppressNotificationsCounter;
        }

        /// <summary>
        /// Allows PropertyChanged events.
        /// </summary>
        /// <returns>The new suppression counter</returns>
        public int AllowNotifications()
        {
            return --SuppressNotificationsCounter;
        }
        #endregion

        #region Protected Helper Methods
        /// <summary>
        /// Checks if the given list of property names are valid in the given type
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <param name="propertyNames">The property types to check</param>
        /// <returns>true if all properties exist and are public</returns>
        protected static bool ValidatePublicPropertyNames(Type type, params string[] propertyNames)
        {
            bool result =
                propertyNames.All(
                propertyName =>
                    type.GetProperties().Any(
                    item => item.Name == propertyName
                        && item.GetGetMethod().IsPublic));

            return result;
        }
        #endregion

        #region Static Dependent Property Initialization
        /// <summary>
        /// Add dependent properties for a given source property
        /// </summary>
        /// <typeparam name="Type">The type</typeparam>
        /// <param name="propertyNameSource">The source property</param>
        /// <param name="propertyNamesDest">The dependent properties</param>
        protected static void AddDependency<Type>(string propertyNameSource, params string[] propertyNamesDest)
        {
            var type = typeof(Type);

            if (!ValidatePublicPropertyNames(type, propertyNameSource))
                throw new ArgumentException("Source property is not a property of the given type", "propertyNameSource");

            if (!ValidatePublicPropertyNames(type, propertyNamesDest))
                throw new ArgumentException("A destination property is not a property of the given type", "propertyNamesDest");

            if (!DependentProperties.ContainsKey(type))
                DependentProperties[type] = new Dictionary<string, List<string>>();

            if (!DependentProperties[type].ContainsKey(propertyNameSource))
                DependentProperties[type][propertyNameSource] = new List<string>();

            foreach (var name in propertyNamesDest)
            {
                if (DependentProperties[type][propertyNameSource].Any(item => item == name))
                    continue;
                DependentProperties[type][propertyNameSource].Add(name);
            }
        }

        /// <summary>
        /// Add a group of interdependent properties (i.e. if one of them changes, all of them change)
        /// </summary>
        /// <typeparam name="Type">The type</typeparam>
        /// <param name="propertyNamesDest">The list of property names</param>
        protected static void AddDependencyGroup<Type>(params string[] propertyNamesDest)
        {
            var type = typeof(Type);

            if (!ValidatePublicPropertyNames(type, propertyNamesDest))
                throw new ArgumentException("A destination property is not a property of the given type", "propertyNamesDest");

            if (!DependentProperties.ContainsKey(type))
                DependentProperties[type] = new Dictionary<string, List<string>>();

            foreach (var propertyNameSource in propertyNamesDest)
            {
                if (!DependentProperties[type].ContainsKey(propertyNameSource))
                    DependentProperties[type][propertyNameSource] = new List<string>();

                foreach (var name in propertyNamesDest)
                {
                    if (name == propertyNameSource)
                        continue;
                    if (DependentProperties[type][propertyNameSource].Any(item => item == name))
                        continue;
                    DependentProperties[type][propertyNameSource].Add(name);
                }
            }
        }
        #endregion

        #region Set Helper Methods
        /// <summary>
        /// Method to set a backing field if the new value is different
        /// </summary>
        /// <typeparam name="Type">The type of the field</typeparam>
        /// <param name="value">The reference to the field</param>
        /// <param name="newValue">The new value</param>
        /// <param name="propertyName">The name of the wrapping property</param>
        protected void Set<Type>(ref Type value, Type newValue, [CallerMemberName] string propertyName = null)
        {
            if (value==null || !value.Equals(newValue))
            {
                if (value == null && newValue == null)
                    return;

                value = newValue;
                RaisePropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// Method to set a backing field if the new value is different; fires a given action on change also
        /// </summary>
        /// <typeparam name="Type">The type of the field</typeparam>
        /// <param name="value">The reference to the field</param>
        /// <param name="newValue">The new value</param>
        /// <param name="additionalActions">The action to fire if the field changed its value</param>
        /// <param name="propertyName">The name of the wrapping property</param>
        protected void Set<Type>(ref Type value, Type newValue, Action additionalActions, [CallerMemberName] string propertyName = null)
        {
            if (value == null || !value.Equals(newValue))
            {
                if (value == null && newValue == null)
                    return;

                value = newValue;
                additionalActions();
                RaisePropertyChanged(propertyName);
            }
        }
        #endregion

        #region INotifyPropertyChanged
        /// <summary>
        /// The PropertyChanged event
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the property changed event; derives the callers' property name automatically
        /// and takes care of firing the events for dependent properties as well
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (SuppressNotificationsCounter > 0)
                return;

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
                if (CallerType != null)
                {
                    if (DependentProperties[CallerType].ContainsKey(propertyName))
                    {
                        var alreadyProcessed = new HashSet<string>();
                        var toProcess = new Queue<string>(DependentProperties[CallerType][propertyName]);
                        while (toProcess.Count > 0)
                        {
                            var prop = toProcess.Dequeue();
                            alreadyProcessed.Add(prop);
                            if (DependentProperties[CallerType].ContainsKey(prop))
                            {
                                foreach (var subProp in DependentProperties[CallerType][prop])
                                {
                                    if (alreadyProcessed.Contains(subProp))
                                        continue;
                                    toProcess.Enqueue(subProp);
                                }
                            }
                        }
                        foreach (var prop in alreadyProcessed)
                            PropertyChanged(this, new PropertyChangedEventArgs(prop));
                    }
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Extension methods for the interface INotifyPropertyChanged
    /// </summary>
    public class NotifyPropertyChangedBinder : IDisposable
    {
        private Dictionary<INotifyPropertyChanged, Dictionary<string, Action>> _bindings;

        /// <summary>
        /// Create binding object
        /// </summary>
        public NotifyPropertyChangedBinder()
        {
            _bindings = new Dictionary<INotifyPropertyChanged, Dictionary<string, Action>>();
        }

        /// <summary>
        /// Bind to a property changed event
        /// </summary>
        /// <typeparam name="T_Type"></typeparam>
        /// <typeparam name="T_Arg"></typeparam>
        /// <param name="itf"></param>
        /// <param name="propAccess"></param>
        /// <param name="action"></param>
        public void BindToPropertyChanged<T_Type, T_Arg>(T_Type itf, System.Linq.Expressions.Expression<Func<T_Type, T_Arg>> propAccess, Action action) where T_Type: INotifyPropertyChanged
        {
            MemberExpression expr = propAccess.Body as MemberExpression;
            if (expr == null)
                throw new ArgumentException("Not a valid MemberExpression!", "propAccess");

            Dictionary<string, Action> binding = null;
            if (!_bindings.TryGetValue(itf, out binding))
            {
                binding = new Dictionary<string, Action>();
                _bindings[itf] = binding;
                itf.PropertyChanged += PropertyChanged;
            }

            binding[expr.Member.Name] = action;
        }

        /// <summary>
        /// Property changed handler
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">PropertyChanged event args</param>
        void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            INotifyPropertyChanged pc = sender as INotifyPropertyChanged;
            Dictionary<string, Action> binding = null;
            if (!_bindings.TryGetValue(pc, out binding))
            {
                pc.PropertyChanged -= PropertyChanged;
                return;
            }
            Action action = null;
            if (binding.TryGetValue(e.PropertyName, out action))
            {
                action();
            }
        }

        /// <summary>
        /// Dispose implementation
        /// </summary>
        public void Dispose()
        {
            foreach (var binding in _bindings)
            {
                binding.Key.PropertyChanged -= PropertyChanged;
            }
        }
    }
}
