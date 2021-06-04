using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.Escaping
{
    /// <summary>
    /// Escapes all characters outside of 0-9, a-z, A-Z
    /// </summary>
    public static class OnlyASCIICharacters
    {
        [ThreadStatic]
        private static StringBuilder _sb; 

        /// <summary>
        /// Encode
        /// </summary>
        /// <param name="source">source string</param>
        /// <returns>encoded string</returns>
        public static string Encode(string source)
        {
            if (_sb == null)
                _sb = new StringBuilder();

            _sb.Clear();
            foreach (var c in source)
            {
                if (c >= '0' && c <= '9'
                    || c >= 'a' && c <= 'z'
                    || c >= 'A' && c <= 'Z')
                    _sb.Append(c);
                else
                {
                    _sb.Append('#');
                    _sb.Append(((uint)c).ToString("x4"));
                }
            }
            return _sb.ToString();
        }

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="source">source string</param>
        /// <returns>decoded string</returns>
        public static string Decode(string source)
        {
            if (_sb == null)
                _sb = new StringBuilder();

            _sb.Clear();
            int len = source.Length;
            for (int idx = 0; idx < len; ++idx)
            {
                var c = source[idx];
                if (c >= '0' && c <= '9'
                    || c >= 'a' && c <= 'z'
                    || c >= 'A' && c <= 'Z')
                    _sb.Append(c);
                else
                {
                    if (c != '#')
                        throw new InvalidOperationException("Unexpected character");
                    ++idx;
                    if ((idx + 4) >= len)
                        throw new InvalidOperationException("Unexpected escape sequence");
                    uint val = uint.Parse(source.Substring(idx, 4), System.Globalization.NumberStyles.HexNumber);
                    _sb.Append((char)val);
                    idx += 3;
                }
            }
            return _sb.ToString();
        }
    }
}
