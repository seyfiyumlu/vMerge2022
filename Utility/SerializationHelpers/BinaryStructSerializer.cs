using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.SerializationHelpers
{
    /// <summary>
    /// Small helper class to read/save plain structs to/from a file.
    /// Must not be used for critical data as it is not versionized at all.
    /// </summary>
    public static class BinaryStructSerializer
    {
        /// <summary>
        /// Reads a given struct from a file
        /// </summary>
        /// <typeparam name="Type">Type</typeparam>
        /// <param name="input">The source struct</param>
        /// <param name="targetPath">The target path</param>
        public static void Save<Type>(Type input, string targetPath) where Type : struct
        {
            int sizePayload = Marshal.SizeOf(typeof(Type));

            byte[] bytes = new byte[sizePayload];
            IntPtr ptr = Marshal.AllocHGlobal(sizePayload);
            try
            {
                Marshal.StructureToPtr(input, ptr, true);
                Marshal.Copy(ptr, bytes, 0, sizePayload);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            File.WriteAllBytes(targetPath, bytes);
        }

        /// <summary>
        /// Reads a given struct from a file
        /// </summary>
        /// <typeparam name="Type">Type</typeparam>
        /// <param name="sourcePath">Source path</param>
        /// <param name="output">The result</param>
        public static void Read<Type>(string sourcePath, out Type output) where Type : struct
        {
            try
            {
                int sizePayload = Marshal.SizeOf(typeof(Type));

                byte[] bytes = new byte[sizePayload];
                File.ReadAllBytes(sourcePath).CopyTo(bytes, 0);

                IntPtr ptr = Marshal.AllocHGlobal(sizePayload);

                try
                {
                    Marshal.Copy(bytes, 0, ptr, sizePayload);
                    output = (Type)Marshal.PtrToStructure(ptr, typeof(Type));
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch (Exception)
            {
                output = default(Type);
            }
        }

        /// <summary>
        /// Reads a given struct from a file
        /// </summary>
        /// <typeparam name="Type">Type</typeparam>
        /// <param name="sourcePath">Source path</param>
        /// <returns>The struct read</returns>
        public static Type Read<Type>(string sourcePath) where Type : struct
        {
            Type dummy = default(Type);
            Read(sourcePath, out dummy);
            return dummy;
        }
    }
}
