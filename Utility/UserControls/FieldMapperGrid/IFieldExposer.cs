using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.UserControls.FieldMapperGrid
{
    /// <summary>
    /// Interface for exposing a named set of fields
    /// </summary>
    public interface IFieldExposer
    {
        /// <summary>
        /// The wrapped item
        /// </summary>
        object InnerItem
        {
            get;
        }

        /// <summary>
        /// Access a contained field by name. Forwards to the field accessor.
        /// </summary>
        /// <param name="fieldName">The field to access</param>
        /// <returns>Field value</returns>
        object this[string fieldName]
        {
            get;
            set;
        }
    }
}
