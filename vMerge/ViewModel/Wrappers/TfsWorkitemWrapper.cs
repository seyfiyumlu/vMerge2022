using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.vMerge.Model;
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.ViewModel.Wrappers
{
    public class TfsWorkItemWrapper : NotifyPropertyChangedImpl
    {
        static TfsWorkItemWrapper()
        {
            NotifyPropertyChangedImpl.AddDependency<TfsWorkItemWrapper>("IsHighlighted", "FontWeight");
        }

        private ITfsWorkItem _tfsWorkItem;
        public ITfsWorkItem TfsWorkItem
        {
            get { return _tfsWorkItem; }
            set { Set(ref _tfsWorkItem, value); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        private bool _isHighlighted;
        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set { Set(ref _isHighlighted, value); }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { Set(ref _isVisible, value); }
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

        private List<ITfsChangeset> _relatedChangesets;
        public List<ITfsChangeset> RelatedChangesets
        {
            get
            {
                if (_relatedChangesets == null)
                {
                    _relatedChangesets = TfsWorkItem.RelatedChangesets;
                }
                return TfsWorkItem.RelatedChangesets;
            }
        }

        private List<ITfsChangeset> _mergedChangesets;
        public List<ITfsChangeset> MergedChangesets
        {
            get
            {
                if (_mergedChangesets==null)
                {
                    _mergedChangesets = new List<ITfsChangeset>();
                }
                return _mergedChangesets;
            }
        }

        public bool RelatedChangesetsLoaded
        {
            get
            {
                return _relatedChangesets != null;
            }
        }

        public int RelatedChangesetCount
        {
            get
            {
                if (_relatedChangesets != null)
                    return _relatedChangesets.Count;
                else
                    return TfsWorkItem.RelatedChangesetCount;
            }
        }

        public IEnumerable<ITfsChangeset> RelatedChangesetsAsEnumerable
        {
            get
            {
                if (_relatedChangesets != null)
                {
                    foreach (var changeset in _relatedChangesets)
                    {
                        yield return changeset;
                    }
                }
                else
                {
                    var result = new List<ITfsChangeset>();
                    foreach (var changeset in Repository.Instance.TfsBridgeProvider.GetRelatedChangesetsForWorkItem(_tfsWorkItem))
                    {
                        result.Add(changeset);
                        yield return changeset;
                    }
                    if (_relatedChangesets == null)
                        _relatedChangesets = result;
                }
            }
        }

        public TfsWorkItemWrapper(ITfsWorkItem source)
            : base(typeof(TfsWorkItemWrapper))
        {
            _tfsWorkItem = source;
        }

        public override string ToString()
        {
            return "#" + TfsWorkItem.WorkItem.Id.ToString() + ", " + TfsWorkItem.WorkItem.Title;
        }
    }
}
