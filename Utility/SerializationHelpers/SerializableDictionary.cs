using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Linq;
using alexbegh.Utility.Helpers.ExtensionMethods;

namespace alexbegh.Utility.SerializationHelpers
{
    /// <summary>
    /// This class provides a serializable Dictionary implementation
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TValue">The value type</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors"), XmlRoot("dictionary")]
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
        : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members
        /// <summary>
        /// Returns the schema (not implemented)
        /// </summary>
        /// <returns>null</returns>
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Reads contents from an XmlReader
        /// </summary>
        /// <param name="reader">The XmlReader</param>
        public void ReadXml(System.Xml.XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");

                reader.ReadStartElement("key");
                TKey key;
                Serializer.XmlDeserialize(reader, out key);
                reader.ReadEndElement();

                var type = typeof(TValue);
                if (typeof(TValue) == typeof(object))
                {
                    type = DeserializeType(reader);
                }

                reader.ReadStartElement("value");
                TValue value;
                Serializer.XmlDeserialize(type, reader, out value);
                reader.ReadEndElement();

                this.Add(key, value);

                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        void SerializeType(System.Xml.XmlWriter writer, Type t)
        {
            writer.WriteStartElement("type");
            writer.WriteStartAttribute("generic");
            writer.WriteValue(t.IsGenericType);
            writer.WriteEndAttribute();
            writer.WriteStartAttribute("name");
            if (t.IsGenericType)
            {
                writer.WriteValue(t.GetGenericTypeDefinition().AssemblyQualifiedName);
            }
            else
            {
                writer.WriteValue(t.AssemblyQualifiedName);
            }
            writer.WriteEndAttribute();
            if (t.IsGenericType)
            {
                writer.WriteStartAttribute("count");
                writer.WriteValue(t.GenericTypeArguments.Length);
                writer.WriteEndAttribute();
                foreach (var subtype in t.GenericTypeArguments)
                {
                    SerializeType(writer, subtype);
                }
            }
            writer.WriteEndElement();
        }

        Type DeserializeType(System.Xml.XmlReader reader)
        {
            Type result = null;
            bool isGeneric = bool.Parse(reader.GetAttribute("generic"));
            string name = reader.GetAttribute("name");
            result = TypeExtensions.GetTypeEx(name);

            if (isGeneric)
            {
                int args = int.Parse(reader.GetAttribute("count"));
                Type[] types = new Type[args];
                reader.ReadStartElement("type");
                while (reader.MoveToNextAttribute()) ;
                for (int i = 0; i < args; ++i)
                {
                    types[i] = DeserializeType(reader);
                }
                result = result.MakeGenericType(types);
                reader.ReadEndElement();
            }
            else
            {
                reader.ReadStartElement("type");
                while (reader.MoveToNextAttribute()) ;
            }
            return result;
        }

        /// <summary>
        /// Writes content to an XmlWriter
        /// </summary>
        /// <param name="writer">The XmlWriter</param>
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                Serializer.XmlSerialize(key, writer);
                writer.WriteEndElement();

                TValue value = this[key];
                if (typeof(TValue) == typeof(object))
                {
                    SerializeType(writer, value == null ? typeof(object) : value.GetType());
                }

                writer.WriteStartElement("value");
                Serializer.XmlSerialize(value, writer);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }
        #endregion
    }
}
