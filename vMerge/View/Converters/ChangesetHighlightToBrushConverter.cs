using alexbegh.Utility.Helpers.Converters;
using System.Windows.Media;

namespace alexbegh.vMerge.View.Converters
{
    class ChangesetHighlightToBrushConverter : BooleanToBrushConverter<ChangesetHighlightToBrushConverter>
    {
        public ChangesetHighlightToBrushConverter()
        {
            BrushWhenFalse = null;
            BrushWhenTrue = new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0));
        }
    }
}
