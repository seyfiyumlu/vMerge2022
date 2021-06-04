using alexbegh.Utility.Helpers.WPFBindings;
using alexbegh.Utility.Managers.Background;
using alexbegh.Utility.Managers.View;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.StudioIntegration.Helpers;
using alexbegh.vMerge.ViewModel;
using alexbegh.vMerge.ViewModel.WorkItems;
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
    [AssociatedViewModel(typeof(WorkItemViewModel), Key="WorkItems")]
    public partial class WorkItemWindow : UserControl
    {
        public WorkItemWindow()
        {
            InitializeComponent();
            this.SizeChanged += WorkItemWindow_SizeChanged;
        }

        void WorkItemWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //LeftButtonsWrapPanel.Width
                //= Math.Max(100, (ButtonGrid.ActualWidth - RightButtonPanel.ActualWidth));
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            ChangeContextMenuImageSourceAccordingToTheme.Process(sender as ContextMenu);
        }
    }
}