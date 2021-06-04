using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace alexbegh.vMerge.View.Converters
{
    class LevelToMarginConverter : MarkupExtension, IValueConverter
    {
        #region Private Fields
        /// <summary>
        /// The instance of the converter (needed for supporting MarkupExtension)
        /// </summary>
        private static LevelToMarginConverter _instance;
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
                _instance = new LevelToMarginConverter();
            return _instance;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts the integer value to a margin
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>Brush</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                int level = (int)value;
                return new Thickness(level * 10, 0, 0, 0);
            }
            return null;
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
