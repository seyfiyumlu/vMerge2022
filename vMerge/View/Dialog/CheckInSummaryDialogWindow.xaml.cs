using Microsoft.VisualStudio.PlatformUI;
using alexbegh.Utility.Managers.View;
using alexbegh.Utility.Windows;
using alexbegh.vMerge.ViewModel.Merge;
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
using System.Windows.Shapes;

namespace alexbegh.vMerge.View.Dialog
{
    /// <summary>
    /// Interaction logic for CheckInSummaryDialogWindow.xaml
    /// </summary>
    [AssociatedViewModel(typeof(CheckInSummaryViewModel), IsModal = true)]
    public partial class CheckInSummaryDialogWindow : DialogWindow
    {
        public CheckInSummaryDialogWindow()
        {
            InitializeComponent();
        }
    }
}
