using System;
using System.Collections.Generic;

namespace alexbegh.Utility.UserControls.FieldMapperGrid
{
    /// <summary>
    /// This interface must be implemented to provide the <see cref="FieldMapperGridViewModel{T}"/> with access
    /// to a certain classes properties
    /// </summary>
    /// <typeparam name="T_Item">The type to provide access to</typeparam>
    public interface IPropertyAccessor<T_Item>
    {
        /// <summary>
        /// The list of possible fields of a certain instance of the item
        /// </summary>
        /// <param name="source">The source</param>
        /// <returns>List of field names</returns>
        IEnumerable<string> AllFieldsOf(T_Item source);

        /// <summary>
        /// Returns the type of a given field
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="fieldName">The field</param>
        /// <returns>The type</returns>
        Type GetType(T_Item source, string fieldName);

        /// <summary>
        /// Gets the value of a certain field for a given instance of an item
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="fieldName">The field name</param>
        /// <returns>The fields data</returns>
        object GetValue(T_Item source, string fieldName);

        /// <summary>
        /// Sets the value of a certain field for a given instance of an item
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="fieldName">The field</param>
        /// <param name="value">The data to set the field to</param>
        void SetValue(T_Item source, string fieldName, object value);
    }
}
