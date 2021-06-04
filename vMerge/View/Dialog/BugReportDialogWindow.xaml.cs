using Microsoft.VisualStudio.PlatformUI;
using alexbegh.Utility.Commands;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.ViewModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace alexbegh.vMerge.View.Dialog
{
    /// <summary>
    /// Interaction logic for BugReportDialogWindow.xaml
    /// </summary>
    [AssociatedViewModel(typeof(BugReportViewModel), IsModal=true)]
    [DoNotStyle]
    public partial class BugReportDialogWindow : DialogWindow
    {
        public BugReportDialogWindow()
        {
            InitializeComponent();
            DataContextChanged += BugReportDialogWindow_DataContextChanged;
        }

        void BugReportDialogWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            dynamic dc = (dynamic)DataContext;
            RelayCommand cmd = dc.SubmitCommand;
            var newCmd = new RelayCommand(
                (o) => { TextBoxChanged(this, null); cmd.Execute(o); },
                (o) => cmd.CanExecute(o)
                );
            SubmitCommand.Command = newCmd;
        }

        private void TextBoxChanged(object sender, TextChangedEventArgs e)
        {
            dynamic dc = (dynamic)DataContext;

            TextRange t = new TextRange(Description.Document.ContentStart,
                                                Description.Document.ContentEnd);
            using(var stream = new MemoryStream())
            {
                t.Save(stream, System.Windows.DataFormats.Rtf);
                stream.Close();
                dc.Description = stream.ToArray();
            }
        }
    }
}
