using alexbegh.Utility.Helpers;
using alexbegh.Utility.Helpers.ViewModel;
using alexbegh.Utility.UserControls.FieldMapperGrid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using alexbegh.Utility.Commands;
using alexbegh.Utility.Helpers.Collections;
using alexbegh.Utility.Managers.View;

namespace alexbegh.vMerge.ViewModel.Configuration
{
    class ColumnConfigurationViewModel : BaseViewModel, IViewModelIsFinishable
    {
        public ColumnConfigurationViewModel(string configurationName, ObservableCollection<FieldMapperGridColumn> columns)
            : base(typeof(ColumnConfigurationViewModel))
        {
            ConfigurationName = configurationName;
            Columns = columns;
            _selectedColumns = new FilteredReadOnlyObservableCollection<FieldMapperGridColumn>(columns, (x) => x.Visible);
            _availableColumns = new FilteredReadOnlyObservableCollection<FieldMapperGridColumn>(columns, (x) => !x.Visible);

            ShowColumnCommand = new RelayCommand((o) => ShowColumn(), (o) => SelectedInvisibleColumn != null);
            HideColumnCommand = new RelayCommand((o) => HideColumn(), (o) => SelectedVisibleColumn != null);
            OKCommand = new RelayCommand((o) => OK());

            MoveSelectedUpCommand = new RelayCommand((o) => MoveSelectedUp(), (o) => SelectedVisibleColumn != null && SelectedVisibleColumn != SelectedColumns.FirstOrDefault());
            MoveSelectedDownCommand = new RelayCommand((o) => MoveSelectedDown(), (o) => SelectedVisibleColumn != null && SelectedVisibleColumn != SelectedColumns.LastOrDefault());
            MoveAvailableUpCommand = new RelayCommand((o) => MoveAvailableUp(), (o) => SelectedInvisibleColumn != null && SelectedInvisibleColumn != AvailableColumns.FirstOrDefault());
            MoveAvailableDownCommand = new RelayCommand((o) => MoveAvailableDown(), (o) => SelectedInvisibleColumn != null && SelectedInvisibleColumn != AvailableColumns.LastOrDefault());
        }

        private RelayCommand _showColumnCommand;
        public RelayCommand ShowColumnCommand
        {
            get { return _showColumnCommand; }
            set { Set(ref _showColumnCommand, value); }
        }

        private RelayCommand _hideColumnCommand;
        public RelayCommand HideColumnCommand
        {
            get { return _hideColumnCommand; }
            set { Set(ref _hideColumnCommand, value); }
        }

        private RelayCommand _okCommand;
        public RelayCommand OKCommand
        {
            get { return _okCommand; }
            set { Set(ref _okCommand, value); }
        }

        private RelayCommand _moveSelectedUpCommand;
        public RelayCommand MoveSelectedUpCommand
        {
            get { return _moveSelectedUpCommand; }
            set { Set(ref _moveSelectedUpCommand, value); }
        }

        private RelayCommand _moveSelectedDownCommand;
        public RelayCommand MoveSelectedDownCommand
        {
            get { return _moveSelectedDownCommand; }
            set { Set(ref _moveSelectedDownCommand, value); }
        }

        private RelayCommand _moveAvailableUpCommand;
        public RelayCommand MoveAvailableUpCommand
        {
            get { return _moveAvailableUpCommand; }
            set { Set(ref _moveAvailableUpCommand, value); }
        }

        private RelayCommand _moveAvailableDownCommand;
        public RelayCommand MoveAvailableDownCommand
        {
            get { return _moveAvailableDownCommand; }
            set { Set(ref _moveAvailableDownCommand, value); }
        }

        private string _configurationName;
        public string ConfigurationName
        {
            get { return _configurationName; }
            set { Set(ref _configurationName, value); }
        }

        private ObservableCollection<FieldMapperGridColumn> _columns;
        public ObservableCollection<FieldMapperGridColumn> Columns
        {
            get { return _columns; }
            private set { Set(ref _columns, value); }
        }

        private FilteredReadOnlyObservableCollection<FieldMapperGridColumn> _selectedColumns;
        public FilteredReadOnlyObservableCollection<FieldMapperGridColumn> SelectedColumns
        {
            get { return _selectedColumns; }
        }

        private FilteredReadOnlyObservableCollection<FieldMapperGridColumn> _availableColumns;
        public FilteredReadOnlyObservableCollection<FieldMapperGridColumn> AvailableColumns
        {
            get { return _availableColumns; }
        }

        private FieldMapperGridColumn _selectedVisibleColumn;
        public FieldMapperGridColumn SelectedVisibleColumn
        {
            get { return _selectedVisibleColumn; }
            set { Set(ref _selectedVisibleColumn, value); }
        }

        private FieldMapperGridColumn _selectedInvisibleColumn;
        public FieldMapperGridColumn SelectedInvisibleColumn
        {
            get { return _selectedInvisibleColumn; }
            set { Set(ref _selectedInvisibleColumn, value); }
        }

        public void ShowColumn()
        {
            var columnToInsert = SelectedInvisibleColumn;
            var oldIndex = AvailableColumns.IndexOf(columnToInsert);
            int newIndex = oldIndex;
            if (newIndex >= (AvailableColumns.Count - 1))
                --newIndex;

            var currentlySelectedIndex = SelectedVisibleColumn != null ? Columns.IndexOf(SelectedVisibleColumn) : -1;
            Columns.Remove(columnToInsert);
            if (currentlySelectedIndex == -1)
                Columns.Add(columnToInsert);
            else
                Columns.Insert(currentlySelectedIndex, columnToInsert);
            columnToInsert.Visible = true;
            SelectedInvisibleColumn = (newIndex >= 0) ? AvailableColumns[newIndex] : null;
        }

        public void HideColumn()
        {
            var oldIndex = SelectedColumns.IndexOf(SelectedVisibleColumn);
            int newIndex = oldIndex;
            if (newIndex >= (SelectedColumns.Count - 1))
                --newIndex;

            var col = SelectedVisibleColumn;
            SelectedVisibleColumn = null;
            col.Visible = false;
            SelectedVisibleColumn = (newIndex >= 0) ? SelectedColumns[newIndex] : null;
        }

        public void MoveSelectedUp()
        {
            var item1 = SelectedVisibleColumn;
            var item2 = SelectedColumns[SelectedColumns.IndexOf(item1) - 1];

            SelectedColumns.Swap(item1, item2);
            SelectedVisibleColumn = item1;
        }

        public void MoveSelectedDown()
        {
            var item1 = SelectedVisibleColumn;
            var item2 = SelectedColumns[SelectedColumns.IndexOf(item1) + 1];

            SelectedColumns.Swap(item1, item2);
            SelectedVisibleColumn = item1;
        }

        public void MoveAvailableUp()
        {
            var item1 = SelectedInvisibleColumn;
            var item2 = AvailableColumns[AvailableColumns.IndexOf(item1) - 1];

            AvailableColumns.Swap(item1, item2);
        }

        public void MoveAvailableDown()
        {
            var item1 = SelectedInvisibleColumn;
            var item2 = AvailableColumns[AvailableColumns.IndexOf(item1) + 1];

            AvailableColumns.Swap(item1, item2);
        }

        private void OK()
        {
            RaiseFinished(true);
        }


        protected override void SaveInternal(object data)
        {
        }

        public event EventHandler<ViewModelFinishedEventArgs> Finished;

        public void RaiseFinished(bool success)
        {
            if (Finished != null)
                Finished(this, new ViewModelFinishedEventArgs(success));
        }
    }
}
