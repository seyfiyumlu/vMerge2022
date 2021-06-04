using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.ExtensionMethods
{
    /// <summary>
    /// Extension methods for System.Type
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Returns the requested type. First searches for correct version, if nothing
        /// found searches without version info
        /// </summary>
        /// <param name="fullTypeName">The assembly qualified name of the type to search</param>
        /// <returns>The type, null if not found</returns>
        public static Type GetTypeEx(string fullTypeName)
        {
            Type type = Type.GetType(fullTypeName);
            if (type != null)
                return type;

            int idx1 = fullTypeName.IndexOf(", Version=");
            int idx2 = fullTypeName.IndexOf(',', idx1 + 1);
            fullTypeName = fullTypeName.Substring(0, idx1) + fullTypeName.Substring(idx2);
            return Type.GetType(fullTypeName);
        }

    }
}
