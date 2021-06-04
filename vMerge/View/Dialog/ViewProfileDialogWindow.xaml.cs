using Microsoft.VisualStudio.PlatformUI;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.ViewModel.Profile;
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
    /// Interaction logic for ViewProfileDialogWindow.xaml
    /// </summary>
    [AssociatedViewModel(typeof(ViewProfileViewModel), IsModal = true)]
    [DoNotStyle()]
    public partial class ViewProfileDialogWindow : DialogWindow
    {
        public ViewProfileDialogWindow()
        {
            InitializeComponent();
        }
    }
}
