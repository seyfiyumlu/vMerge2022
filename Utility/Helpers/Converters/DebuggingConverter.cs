using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace alexbegh.Utility.Helpers.Converters
{
    /// <summary>
    /// Can be used to debug bindings.
    /// </summary>
    public class DebuggingConverter : IValueConverter
    {
        /// <summary>
        /// Called when the binding value is about to be converted
        /// </summary>
        /// <param name="value">The source value</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>Unmodified value</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value; // Add the breakpoint here!!
        }

        /// <summary>
        /// Performs the backwards conversion; not implemented
        /// </summary>
        /// <param name="value">Not used</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>nothing; throws exception</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("This method should never be called");
        }
    }
}
