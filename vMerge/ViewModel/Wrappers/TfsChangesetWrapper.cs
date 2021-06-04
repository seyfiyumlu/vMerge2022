using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.ViewModel.Wrappers
{
    public class TfsChangesetWrapper : NotifyPropertyChangedImpl
    {
        static TfsChangesetWrapper()
        {
            NotifyPropertyChangedImpl.AddDependency<TfsChangesetWrapper>("IsHighlighted", "FontWeight");
        }

        private ITfsChangeset _tfsChangeset;
        public ITfsChangeset TfsChangeset
        {
            get { return _tfsChangeset; }
            set { Set(ref _tfsChangeset, value); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { Set(ref _isVisible, value); }
        }

        private bool _isHighlighted;
        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set { Set(ref _isHighlighted, value); }
        }

        private bool _hasWarning;
        public bool HasWarning
        {
            get { return _hasWarning; }
            set { Set(ref _hasWarning, value); }
        }

        private string _warningText;
        public string WarningText
        {
            get { return _warningText; }
            set { Set(ref _warningText, value); }
        }

        public string FontWeight
        {
            get { return IsHighlighted ? "Bold" : "Normal"; }
        }

        public TfsChangesetWrapper(ITfsChangeset source)
            : base(typeof(TfsChangesetWrapper))
        {
            _tfsChangeset = source;
        }

        public override string ToString()
        {
            return "Changeset #" + (TfsChangeset != null ? TfsChangeset.Changeset.ChangesetId : -1).ToString() + " - " + (TfsChangeset != null ? TfsChangeset.Description : "?");
        }
    }
}
