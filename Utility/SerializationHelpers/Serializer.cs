using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.UserControls.FieldMapperGrid.Control;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace alexbegh.Utility.SerializationHelpers
{
    /// <summary>
    /// This class takes care of serialization.
    /// See also: <seealso cref="RegisterForSerializationAttribute"/>, <seealso cref="RegisterForSerializationExplicit"/>
    /// </summary>
    public class Serializer
    {
        /// <summary>
        /// The dictionary of already constructed serializers
        /// </summary>
        private static Dictionary<Type, XmlSerializer> Serializers
        {
            get;
            set;
        }

        /// <summary>
        /// The list of known types
        /// </summary>
        private static HashSet<Type> Types;

        /// <summary>
        /// The static constructur
        /// </summary>
        static Serializer()
        {
            RegisterForSerializationExplicit.Register(typeof(ObservableCollection<>));
            Serializers = new Dictionary<Type, XmlSerializer>();
            Types = new HashSet<Type>(GatherTypeList(Assembly.GetExecutingAssembly()));
        }

        /// <summary>
        /// Registers all possible types from an assembly
        /// </summary>
        /// <param name="assembly">The assembly</param>
        public static void RegisterAssemblyTypes(Assembly assembly)
        {

            foreach (var type in GatherTypeList(assembly))
            {
                if (!Types.Contains(type))
                    Types.Add(type);
            }
        }

        /// <summary>
        /// Registers all possible types of the calling assembly
        /// </summary>
        public static void RegisterAssemblyTypes()
        {
            RegisterAssemblyTypes(Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Serialize a given object to targetPath
        /// </summary>
        /// <typeparam name="T_Type">The type of obj</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="targetPath">The target path to serialize to</param>
        public static void XmlSerialize<T_Type>(T_Type obj, string targetPath)
        {
            var serializer = GetSerializerFor(typeof(T_Type));
            using (var writer = XmlWriter.Create(targetPath))
            {
                XmlSerialize(obj, writer);
            }
        }

        /// <summary>
        /// Serialize a given object to an XmlWriter
        /// </summary>
        /// <typeparam name="T_Type">The type of obj</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="writer">The XmlWriter</param>
        public static void XmlSerialize<T_Type>(T_Type obj, XmlWriter writer)
        {
            var serializer = GetSerializerFor(obj.GetType());
            if (serializer != null)
            {
                serializer.Serialize(writer, obj);
            }

        }

        /// <summary>
        /// Deserializes an object from a given targetPath
        /// </summary>
        /// <typeparam name="T_Type">The type of the object to deserialize</typeparam>
        /// <param name="targetPath">The targetPath</param>
        /// <param name="obj">The resulting object</param>
        public static void XmlDeserialize<T_Type>(string targetPath, out T_Type obj)
        {
            var serializer = GetSerializerFor(typeof(T_Type));
            using (var reader = XmlReader.Create(targetPath))
            {
                XmlDeserialize(reader, out obj);
            }
        }

        /// <summary>
        /// Deserializes an object from an XmlReader
        /// </summary>
        /// <typeparam name="T_Type">The type of the object to deserialize</typeparam>
        /// <param name="reader">The XmlReader</param>
        /// <param name="obj">The resulting object</param>
        public static void XmlDeserialize<T_Type>(XmlReader reader, out T_Type obj)
        {
            var serializer = GetSerializerFor(typeof(T_Type));
            obj = (T_Type)serializer.Deserialize(reader);
        }

        /// <summary>
        /// Deserializes an object from an XmlReader
        /// </summary>
        /// <typeparam name="T_Type">The type of the object to deserialize</typeparam>
        /// <param name="type">The type to read</param>
        /// <param name="reader">The XmlReader</param>
        /// <param name="obj">The resulting object</param>
        public static void XmlDeserialize<T_Type>(Type type, XmlReader reader, out T_Type obj)
        {
            var serializer = GetSerializerFor(type);
            if (serializer == null)
            {
                throw new Exception("Error while XmlDeserialize.");
            }

            obj = (T_Type)serializer.Deserialize(reader);
        }

        /// <summary>
        /// Helper method: returns a XmlSerializer for a given type, constructing it if necessary
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>An XmlSerializer</returns>
        public static XmlSerializer GetSerializerFor(Type type)
        {
            XmlSerializer result = null;
            if (Serializers.TryGetValue(type, out result))
                return result;
            try
            {
                var knownTypes = new Type[] { typeof(PrivateFieldData) };
                result = new XmlSerializer(type, knownTypes);
                Serializers[type] = result;
                return result;
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(SimpleLogLevel.Error ,$"Unable to serialize type : {type.Namespace} : {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetch all types from a given assembly.
        /// Tries to include derived types (from generic types) as well by reflection.
        /// </summary>
        /// <param name="assembly">The assembly to check</param>
        /// <returns>List of types</returns>
        public static Type[] GatherTypeList(Assembly assembly)
        {
            var assemblyTypes = assembly.GetTypes()
                .Where(item => item.GetCustomAttribute(typeof(SerializableAttribute)) != null
                               || item.GetCustomAttribute(typeof(RegisterForSerializationAttribute)) != null)
                .Where(item => !item.IsSubclassOf(typeof(Exception)))
                .ToArray();
            var result = new List<Type>(assemblyTypes);

            if (RegisterForSerializationExplicit.Types != null)
            {
                foreach (var type in RegisterForSerializationExplicit.Types)
                {
                    if (type.IsGenericType)
                    {
                        if (type.GetGenericArguments().Length == 1)
                        {
                            foreach (var otherType in assemblyTypes)
                            {
                                if (otherType.IsGenericType)
                                    continue;
                                try
                                {
                                    result.Add(type.MakeGenericType(new Type[] { otherType }));
                                }
                                catch (Exception)
                                {
                                    SimpleLogger.Log(SimpleLogLevel.Error, "Failed to create generic instance for " + otherType.ToString() + "/" + type.ToString());
                                    
                                }
                            }
                        }
                    }
                }
            }

            return
                result.Where(item => item.GetGenericArguments().Length == item.GenericTypeArguments.Length).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T_Type"></typeparam>
        /// <param name="obj"></param>
        /// <param name="targetPath"></param>
        public static void JsonSerialize<T_Type>(T_Type obj, string targetPath)
        {
            //SimpleLogger.Log(SimpleLogLevel.Info, "Serialize to: " + targetPath);
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(targetPath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, obj);
            }
        }

    }
}
