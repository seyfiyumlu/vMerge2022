using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ITfsQueryItem
    {
        ITfsQueryFolder Parent { get; }

        string Title { get; }

        int Level { get; }

        string QualifiedTitle { get; }
    }

    public interface ITfsQuery : ITfsQueryItem
    {
        QueryDefinition QueryDefinition { get; }
        IEnumerable<ITfsWorkItem> GetResults();
    }

    public interface ITfsQueryFolder : ITfsQueryItem
    {
        List<ITfsQueryItem> Children { get; }

        IEnumerable<ITfsQueryItem> All { get; }
    }
}
