using System.IO;

namespace qbusSRL.Utility.Helpers.Encoding
{
    /// <summary>
    /// Interface for checking for utf8.
    /// </summary>
    public interface IUtf8Checker
    {
        /// <summary>
        /// Check if file is utf8 encoded.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>true if utf8 encoded, otherwise false.</returns>
        bool Check(string fileName);

        /// <summary>
        /// Check if stream is utf8 encoded.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>true if utf8 encoded, otherwise false.</returns>
        bool IsUtf8(Stream stream);
    }
}