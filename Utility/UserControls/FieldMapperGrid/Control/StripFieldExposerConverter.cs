using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace alexbegh.Utility.UserControls.FieldMapperGrid.Control
{
    /// <summary>
    /// This converter transmogrifies the WPF binding
    /// </summary>
    public class StripFieldExposerConverter : IValueConverter
    {
        private IFieldMapperGridViewModel Host
        {
            get;
            set;
        }

        /// <summary>
        /// Constructs an instance
        /// </summary>
        /// <param name="host"></param>
        internal StripFieldExposerConverter(IFieldMapperGridViewModel host)
        {
            Host = host;
        }

        /// <summary>
        /// WPF converter to convert a FieldMapperGridViewModel DataContext to a list of MenuItems
        /// </summary>
        /// <param name="value">The source value (FieldMapperGridViewModel)</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>A list of MenuItems</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value;
            if (item != null)
            {
                foreach (var current in (System.Collections.ICollection)Host.Elements)
                {
                    IFieldExposer currentItem = (IFieldExposer)current;
                    if (currentItem.InnerItem == item)
                        return currentItem;
                }
                return null;
            }
            return Binding.DoNothing;
        }

        /// <summary>
        /// Performs the backwards conversion; not implemented
        /// </summary>
        /// <param name="value">Not used</param>
        /// <param name="targetType">Not used</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Not used</param>
        /// <returns>nothing; throws exception</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as IFieldExposer;
            if (item != null)
            {
                return item.InnerItem;
            }
            return Binding.DoNothing;
        }
    }
}
