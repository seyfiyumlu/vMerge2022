using System.Collections.ObjectModel;
using System.Windows;

namespace alexbegh.Utility.UserControls.FieldMapperGrid
{
    /// <summary>
    /// The publicly visible interface of <see cref="FieldMapperGridViewModel{T}" />
    /// </summary>
    public interface IFieldMapperGridViewModel
    {
        /// <summary>
        /// The list of contained elements
        /// </summary>
        object Elements { get; }

        /// <summary>
        /// The internally selected elements
        /// </summary>
        ObservableCollection<object> InternalSelectedElements { get; }

        /// <summary>
        /// The list of columns
        /// </summary>
        ObservableCollection<FieldMapperGridColumn> Columns { get; }

        /// <summary>
        /// Meta data
        /// </summary>
        object MetaData { get; set; }

        /// <summary>
        /// Returns the value of a given item and column
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="columnName">The column</param>
        /// <returns>The value</returns>
        object GetValueOf(object item, string columnName);

        /// <summary>
        /// Raises the "ColumnSettingsChanged" event
        /// </summary>
        void RaiseColumnSettingsChanged();
    }
}
