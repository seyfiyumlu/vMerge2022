using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using alexbegh.Utility.Managers.View;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.ViewModel.Merge;
using System.ComponentModel;

namespace alexbegh.vMerge.View.Dialog
{
    /// <summary>
    /// Interaction logic for ModalMergeDialog.xaml
    /// </summary>
    [AssociatedViewModel(typeof(PrepareMergeViewModel), Key = "Modal", IsModal=true)]
    public partial class ModalMergeDialogWindow : DialogWindow
    {
        public ModalMergeDialogWindow()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            this.Initialized += (o, a) =>
            {
                var data = Repository.Instance.Settings.FetchSettings<string>(Constants.Settings.ModalMergeDialogWindowSettingsKey);
                Window.GetWindow(this).DeserializeFromString(data);
            };
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var data = ViewSettingsSerializer.SerializeToString(this);
            Repository.Instance.Settings.SetSettings(Constants.Settings.ModalMergeDialogWindowSettingsKey, data);
            base.OnClosing(e);
        }
    }
}
