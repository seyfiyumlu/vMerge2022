using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace alexbegh.Utility.Managers.View
{
    /// <summary>
    /// This class is applied on a view implementing class.
    /// It denotes the ViewModel it applies to as a mandatory constructor parameter.
    /// Additional parameters allow to state if the view wants to display
    /// as a modal dialog and more.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AssociatedViewModelAttribute : Attribute
    {
        #region Properties
        /// <summary>
        /// Default view for the ViewModel?
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// View should be displayed as modal?
        /// </summary>
        public bool IsModal { get; set; }

        /// <summary>
        /// The type of the associated view model
        /// </summary>
        public Type AssociatedViewModel { get; private set; }

        /// <summary>
        /// A factory method instancing this view
        /// </summary>
        public Func<ContentControl> FactoryMethod { get; set; }

        /// <summary>
        /// The views' identifier (can be set to distinguish multiple applicable
        /// views for the same ViewModel)
        /// </summary>
        public string Key { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs an instance of this attribute.
        /// </summary>
        /// <param name="associatedViewModel">The ViewModel type this view applies to</param>
        public AssociatedViewModelAttribute(Type associatedViewModel)
        {
            AssociatedViewModel = associatedViewModel;
        }
        #endregion
    }

}
