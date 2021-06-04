using Microsoft.VisualStudio.PlatformUI;
using alexbegh.Utility.Managers.View;
using alexbegh.Utility.Windows;
using alexbegh.vMerge.ViewModel.Configuration;

namespace alexbegh.vMerge.View.Dialog
{
    /// <summary>
    /// Interaction logic for ConfigureColumnsDialogWindow.xaml
    /// </summary>
    [AssociatedViewModel(typeof(ColumnConfigurationViewModel), IsModal = true)]
    public partial class ConfigureColumnsDialogWindow : DialogWindow
    {
        public ConfigureColumnsDialogWindow()
        {
            InitializeComponent();
        }
    }
}
