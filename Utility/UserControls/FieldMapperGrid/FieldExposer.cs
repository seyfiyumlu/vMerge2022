using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Windows.Data;

namespace alexbegh.Utility.UserControls.FieldMapperGrid
{
    /// <summary>
    /// This class exposes the access to a given field using an indexer implementation.
    /// It is used by the FieldMapperGridViewModel to wrap the contained outer type to a
    /// ViewModel compatible type.
    /// </summary>
    /// <typeparam name="T_Item">The type to wrap/expose</typeparam>
    public class FieldExposer<T_Item> : DynamicObject, IFieldExposer, INotifyPropertyChanged
    {
        #region Private Properties
        /// <summary>
        /// The data accessor
        /// </summary>
        private IPropertyAccessor<T_Item> Accessor { get; set; }

        /// <summary>
        /// The cached values
        /// </summary>
        private Dictionary<string, object> CachedValues { get; set; }
        #endregion

        #region Public Properties
        /// <summary>
        /// The contained item
        /// </summary>
        public T_Item InnerItem { get; private set; }

        /// <summary>
        /// The contained item as object
        /// </summary>
        object IFieldExposer.InnerItem
        {
            get { return InnerItem; }
        }
        #endregion

        #region Public Events
        /// <summary>
        /// INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs this type
        /// </summary>
        /// <param name="accessor">The data accessor</param>
        /// <param name="item">The contained item</param>
        public FieldExposer(IPropertyAccessor<T_Item> accessor, T_Item item)
        {
            Accessor = accessor;
            InnerItem = item;
            CachedValues = new Dictionary<string, object>();
            if (InnerItem is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)InnerItem).PropertyChanged +=
                    (o, a) =>
                    {
                        if (string.IsNullOrEmpty(a.PropertyName))
                            CachedValues.Clear();
                        else
                            CachedValues.Remove(a.PropertyName);

                        if (PropertyChanged != null)
                            PropertyChanged(this, a);
                    };
            }
        }
        #endregion

        /// <summary>
        /// Summary:
        ///     Provides the implementation for operations that get member values. Classes
        ///     derived from the System.Dynamic.DynamicObject class can override this method
        ///     to specify dynamic behavior for operations such as getting a value for a
        ///     property.
        ///
        /// Parameters:
        ///   binder:
        ///     Provides information about the object that called the dynamic operation.
        ///     The binder.Name property provides the name of the member on which the dynamic
        ///     operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty)
        ///     statement, where sampleObject is an instance of the class derived from the
        ///     System.Dynamic.DynamicObject class, binder.Name returns "SampleProperty".
        ///     The binder.IgnoreCase property specifies whether the member name is case-sensitive.
        ///
        ///   result:
        ///     The result of the get operation. For example, if the method is called for
        ///     a property, you can assign the property value to result.
        ///
        /// Returns:
        ///     true if the operation is successful; otherwise, false. If this method returns
        ///     false, the run-time binder of the language determines the behavior. (In most
        ///     cases, a run-time exception is thrown.)
        /// </summary>
        /// <param name="binder">See explanation in summary</param>
        /// <param name="result">See explanation in summary</param>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];
            return true;
        }

        /// <summary>
        /// Summary:
        ///     Provides the implementation for operations that set member values. Classes
        ///     derived from the System.Dynamic.DynamicObject class can override this method
        ///     to specify dynamic behavior for operations such as setting a value for a
        ///     property.
        ///
        /// Parameters:
        ///   binder:
        ///     Provides information about the object that called the dynamic operation.
        ///     The binder.Name property provides the name of the member to which the value
        ///     is being assigned. For example, for the statement sampleObject.SampleProperty
        ///     = "Test", where sampleObject is an instance of the class derived from the
        ///     System.Dynamic.DynamicObject class, binder.Name returns "SampleProperty".
        ///     The binder.IgnoreCase property specifies whether the member name is case-sensitive.
        ///
        ///   value:
        ///     The value to set to the member. For example, for sampleObject.SampleProperty
        ///     = "Test", where sampleObject is an instance of the class derived from the
        ///     System.Dynamic.DynamicObject class, the value is "Test".
        ///
        /// Returns:
        ///     true if the operation is successful; otherwise, false. If this method returns
        ///     false, the run-time binder of the language determines the behavior. (In most
        ///     cases, a language-specific run-time exception is thrown.)
        /// </summary>
        /// <param name="binder">See explanation in summary</param>
        /// <param name="value">See explanation in summary</param>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }
        #region Indexer
        /// <summary>
        /// Access a contained field by name. Forwards to the field accessor.
        /// </summary>
        /// <param name="fieldName">The field to access</param>
        /// <returns>Field value</returns>
        public object this[string fieldName]
        {
            get
            {
                object res = null;
                if (CachedValues.TryGetValue(fieldName, out res))
                {
                    return res;
                }
                res = Accessor.GetValue(InnerItem, fieldName);
                CachedValues[fieldName] = res;
                return res;
            }
            set
            {
                if (Accessor.GetValue(InnerItem, fieldName) != value)
                {
                    CachedValues[fieldName] = value;
                    Accessor.SetValue(InnerItem, fieldName, value);
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
                }
            }
        }
        #endregion
    }

}
