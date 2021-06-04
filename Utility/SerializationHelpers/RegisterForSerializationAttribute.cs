using System;

namespace alexbegh.Utility.SerializationHelpers
{
    /// <summary>
    /// Tags a class for serialization inclusion <see cref="Serializer"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RegisterForSerializationAttribute : Attribute
    {
    }
}
