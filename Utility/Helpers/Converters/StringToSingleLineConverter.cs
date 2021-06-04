using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace alexbegh.Utility.Helpers.Converters
{
    /// <summary>
    /// Converts a boolean to visibility and back
    /// </summary>
    public sealed class StringToSingleLineConverter : IValueConverter
    {
        /// <summary>
        /// Convert a string to a single line (removing \r\n characters)
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>Visibility</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null) ? null : (value.ToString()).Replace("\r\n", "␊").Replace("\n", "␊").Replace("\r", "␊");
        }

        /// <summary>
        /// Convert visibility back to boolean
        /// </summary>
        /// <param name="value">The visibility</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>true or false</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}