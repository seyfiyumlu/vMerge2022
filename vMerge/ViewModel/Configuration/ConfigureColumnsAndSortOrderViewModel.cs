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
    class ConfigureColumnsAndSortOrderViewModel : BaseViewModel, IViewModelIsFinishable
    {
        public ConfigureColumnsAndSortOrderViewModel(string configurationName, ObservableCollection<FieldMapperGridColumn> columns)
            : base(typeof(ConfigureColumnsAndSortOrderViewModel))
        {
            ConfigurationName = configurationName;
            Columns = new ObservableCollection<FieldMapperGridColumn>(columns);
            _selectedColumns = new FilteredReadOnlyObservableCollection<FieldMapperGridColumn>(columns, (x) => x.Visible);
            _availableColumns = columns;

            ShowColumnCommand = new RelayCommand((o) => ShowColumn(), (o) => AvailableColumn != null);
            HideColumnCommand = new RelayCommand((o) => HideColumn(), (o) => SelectedColumn != null);
            OKCommand = new RelayCommand((o) => OK());
            Cancelled = true;

            //MoveSelectedUpCommand = new RelayCommand((o) => MoveSelectedUp(), (o) => SelectedVisibleColumn != null && SelectedVisibleColumn != SelectedColumns.FirstOrDefault());
            //MoveSelectedDownCommand = new RelayCommand((o) => MoveSelectedDown(), (o) => SelectedVisibleColumn != null && SelectedVisibleColumn != SelectedColumns.LastOrDefault());
            //MoveAvailableUpCommand = new RelayCommand((o) => MoveAvailableUp(), (o) => SelectedInvisibleColumn != null && SelectedInvisibleColumn != AvailableColumns.FirstOrDefault());
            //MoveAvailableDownCommand = new RelayCommand((o) => MoveAvailableDown(), (o) => SelectedInvisibleColumn != null && SelectedInvisibleColumn != AvailableColumns.LastOrDefault());
        }

        public bool Cancelled { get; private set; }

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

        private ObservableCollection<FieldMapperGridColumn> _availableColumns;
        public ObservableCollection<FieldMapperGridColumn> AvailableColumns
        {
            get { return _availableColumns; }
        }

        private FieldMapperGridColumn _selectedColumn;
        public FieldMapperGridColumn SelectedColumn
        {
            get { return _selectedColumn; }
            set { Set(ref _selectedColumn, value); }
        }

        private FieldMapperGridColumn _availableColumn;
        public FieldMapperGridColumn AvailableColumn
        {
            get { return _availableColumn; }
            set { Set(ref _availableColumn, value); }
        }

        public void ShowColumn()
        {
            var columnToInsert = AvailableColumn;
            var oldIndex = AvailableColumns.IndexOf(columnToInsert);
            int newIndex = oldIndex;
            if (newIndex >= (AvailableColumns.Count - 1))
                --newIndex;

            var currentlySelectedIndex = SelectedColumn != null ? Columns.IndexOf(SelectedColumn) : -1;
            Columns.Remove(columnToInsert);
            if (currentlySelectedIndex == -1)
                Columns.Add(columnToInsert);
            else
                Columns.Insert(currentlySelectedIndex, columnToInsert);
            columnToInsert.Visible = true;
            AvailableColumn = (newIndex >= 0) ? AvailableColumns[newIndex] : null;
        }

        public void HideColumn()
        {
            var oldIndex = SelectedColumns.IndexOf(SelectedColumn);
            int newIndex = oldIndex;
            if (newIndex >= (SelectedColumns.Count - 1))
                --newIndex;

            var col = SelectedColumn;
            SelectedColumn = null;
            col.Visible = false;
            SelectedColumn = (newIndex >= 0) ? SelectedColumns[newIndex] : null;
        }

        public void DragDrop(object sourceHost, FieldMapperGridColumn source, object targetHost, FieldMapperGridColumn target, out FieldMapperGridColumn showAvailable, out FieldMapperGridColumn showSelected)
        {
            showAvailable = null;
            showSelected = null;
            if (sourceHost == AvailableColumns && targetHost == AvailableColumns)
                return;

            if (sourceHost == AvailableColumns && targetHost == SelectedColumns && source != target)
            {
                source.Visible = true;
                AvailableColumns.Remove(source);
                if (target == null)
                    AvailableColumns.Add(source);
                else
                    AvailableColumns.Insert(AvailableColumns.IndexOf(target), source);
            }
            else if (sourceHost == SelectedColumns && targetHost == AvailableColumns)
            {
                source.Visible = false;
                showAvailable = source;
            }
            else if (source != target)
            {
                AvailableColumns.Remove(source);
                if (target == null)
                    AvailableColumns.Add(source);
                else
                    AvailableColumns.Insert(AvailableColumns.IndexOf(target), source);
            }
        }

        private void OK()
        {
            Cancelled = false;
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
