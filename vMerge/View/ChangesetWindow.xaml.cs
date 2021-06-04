using alexbegh.Utility.Helpers.WPFBindings;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.StudioIntegration.Helpers;
using alexbegh.vMerge.ViewModel;
using alexbegh.vMerge.ViewModel.Changesets;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace alexbegh.vMerge.View
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    [AssociatedViewModel(typeof(ChangesetViewModel), Key="Changesets")]
    public partial class ChangesetWindow : UserControl
    {
        public ChangesetWindow()
        {
            InitializeComponent();
            try
            {
                Repository.Instance.Settings.LoadSettings(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vMerge.settings"));
            }
            catch (Exception)
            {
            }
            this.GotFocus += OnControlGotFocus;
        }

        private void OnControlGotFocus(object sender, RoutedEventArgs e)
        {
            var defaultStyle = (Style)this.FindResource(typeof(ContextMenu));
        }

        private void ShowLoadMergeProfilesMenu(object sender, RoutedEventArgs e)
        {
            LoadProfilesMenu.PlacementTarget = this;
            LoadProfilesMenu.DataContext = DataContext;
            LoadProfilesMenu.IsOpen = true;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            ChangeContextMenuImageSourceAccordingToTheme.Process(sender as ContextMenu);
        }
    }
}