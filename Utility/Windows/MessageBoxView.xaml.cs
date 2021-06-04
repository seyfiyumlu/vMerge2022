using Microsoft.VisualStudio.PlatformUI;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.Managers.View;

namespace alexbegh.Utility.Windows
{
    /// <summary>
    /// Interaction logic for MessageBoxView.xaml
    /// </summary>
    [AssociatedViewModel(typeof(MessageBoxViewModel), IsModal=true, IsDefault=true)]
    public partial class MessageBoxView : DialogWindow
    {
        /// <summary>
        /// Initializes the view
        /// </summary>
        public MessageBoxView()
        {
            InitializeComponent();
        }
    }
}
