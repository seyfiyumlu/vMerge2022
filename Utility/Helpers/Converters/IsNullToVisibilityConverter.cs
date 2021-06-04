using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace alexbegh.Utility.Helpers.Converters
{
    /// <summary>
    /// !null -> Visible
    /// </summary>
    public class IsNotNullToVisibilityConverter : MarkupExtension, IValueConverter
    {
        #region Private Fields
        /// <summary>
        /// The instance of the converter (needed for supporting MarkupExtension)
        /// </summary>
        private static IsNotNullToVisibilityConverter _instance;
        #endregion

        #region Public Override Methods
        /// <summary>
        /// Provide the current instance (MarkupExtension)
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_instance == null)
                _instance = new IsNotNullToVisibilityConverter();
            return _instance;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts the boolean value to a brush
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>Brush</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Not Implemented
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
        #endregion
    }
}
