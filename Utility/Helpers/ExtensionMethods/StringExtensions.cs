using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.ExtensionMethods
{
    /// <summary>
    /// Extension methods for "string" class
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Masks a given string
        /// </summary>
        /// <param name="source">The string to mask</param>
        /// <param name="mask">The mask ('_' is a source character, anything else is a mask parameter, _ can be masked by prepending it with an backslash)</param>
        /// <returns>The result</returns>
        public static string Mask(this string source, string mask)
        {
            var res = new StringBuilder();
            int sourcePos = 0;
            for(int maskPos = 0; maskPos<mask.Length && sourcePos<source.Length; ++maskPos)
            {
                char s = source[sourcePos];
                char c = mask[maskPos];
                if (c=='_')
                {
                    res.Append(s);
                    ++sourcePos;
                }
                else if (c == '\\')
                {
                    char next = mask[++maskPos];
                    if (next == s)
                        ++sourcePos;
                    res.Append(next);
                }
                else
                {
                    if (c == s)
                        ++sourcePos;
                    res.Append(c);
                }
            }
            return res.ToString();
        }
    }
}
