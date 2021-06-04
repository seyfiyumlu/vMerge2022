using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace alexbegh.Utility.UserControls.FieldMapperGrid.Control
{
    /// <summary>
    /// Taken from http://paulstovell.com/blog/dynamic-datagrid
    /// Thanks!
    /// </summary>
    public class CustomBoundColumn : DataGridBoundColumn
    {
        /// <summary>
        /// The content template for this column
        /// </summary>
        public DataTemplate ContentTemplate { get; set; }

        /// <summary>
        /// WPF method. Generates an element
        /// </summary>
        /// <param name="cell">The cell</param>
        /// <param name="dataItem">The data item</param>
        /// <returns>A FrameworkElement</returns>
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var binding = new Binding(((Binding)Binding).Path.Path);
            binding.Source = dataItem;

            var content = new ContentControl();
            content.ContentTemplate = ContentTemplate;
            content.SetBinding(ContentControl.ContentProperty, binding);
            return content;
        }

        /// <summary>
        /// WPF method. Generates an element
        /// </summary>
        /// <param name="cell">The cell</param>
        /// <param name="dataItem">The data item</param>
        /// <returns>A FrameworkElement</returns>
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            return GenerateElement(cell, dataItem);
        }
    }
}
