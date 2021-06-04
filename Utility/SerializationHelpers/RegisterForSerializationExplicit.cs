using System;
using System.Collections.Generic;

namespace alexbegh.Utility.SerializationHelpers
{
    /// <summary>
    /// Explicitly registers a type for serialization.
    /// Usage:
    /// <example>
    /// class MyClass
    /// {
    ///     static MyClass()
    ///     {
    ///         new RegisterForSerializationExplicit(typeof(List{string}))
    ///     }
    /// }
    /// </example>
    /// </summary>
    public static class RegisterForSerializationExplicit
    {
        internal static List<Type> Types { get; private set; }

        /// <summary>
        /// Constructs an instance
        /// </summary>
        /// <param name="t">Type type to register</param>
        public static void Register(Type t)
        {
            if (Types == null)
                Types = new List<Type>();
            Types.Add(t);
        }
    }
}
