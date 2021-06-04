using alexbegh.Utility.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.ViewModel
{
    public class MultiLineString : INotifyPropertyChanged
    {
        private string _content;

        public string Value
        {
            get { return _content; }
            set { _content = value; }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { if (value != _isExpanded) { _isExpanded = value; RaisePropertyChanged("IsExpanded"); } }
        }

        private void RaisePropertyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }

        public RelayCommand ToggleCommand
        {
            get;
            set;
        }

        public MultiLineString(string source)
        {
            _content = source;
            ToggleCommand = new RelayCommand((o) => IsExpanded = !IsExpanded, (o) => Value.Contains('\r') || Value.Contains('\n'));
        }

        public override string ToString()
        {
            return _content;
        }

        public override int GetHashCode()
        {
            return _content.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return _content.Equals(obj);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
