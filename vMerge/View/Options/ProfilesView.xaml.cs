using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.ViewModel.Options;
using System.Windows.Controls;

namespace alexbegh.vMerge.View.Options
{
    /// <summary>
    /// Interaction logic for ProfilesView.xaml
    /// </summary>
    [DoNotStyle()]
    [AssociatedViewModel(typeof(ProfilesViewModel))]
    public partial class ProfilesView : UserControl
    {
        public ProfilesView()
        {
            InitializeComponent();

            profilesGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("TeamProject", System.ComponentModel.ListSortDirection.Ascending));
            profilesGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
        }
    }
}
