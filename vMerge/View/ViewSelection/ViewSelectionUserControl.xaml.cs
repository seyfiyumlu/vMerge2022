using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.ViewModel.ViewSelection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using alexbegh.vMerge.StudioIntegration.Framework;
using alexbegh.Utility.Windows;

namespace alexbegh.vMerge.View.ViewSelection
{
    /// <summary>
    /// Interaction logic for ViewSelectionUserControl.xaml
    /// </summary>
    [AssociatedViewModel(typeof(ViewSelectionViewModel), Key = "Changesets")]
    public partial class ViewSelectionUserControl : UserControl
    {
        public ViewSelectionUserControl()
        {
            InitializeComponent();

        }
    }
}
