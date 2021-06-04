using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.Model.Implementation
{
    public class TfsQueryFolder : ITfsQueryFolder
    {
        public ITfsQueryFolder Parent
        {
            get;
            set;
        }

        public List<ITfsQueryItem> Children
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string QualifiedTitle
        {
            get
            {
                string result = Title;
                ITfsQueryFolder current = Parent;
                while (current != null)
                {
                    result = current.Title + "/" + result;
                    current = current.Parent;
                }
                return result;
            }
        }

        public int Level
        {
            get;
            set;
        }

        public IEnumerable<ITfsQueryItem> All
        {
            get
            {
                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        yield return child;
                        if (child is TfsQueryFolder)
                        {
                            foreach (var item in (child as TfsQueryFolder).All)
                                yield return item;
                        }
                    }
                }
            }
        }
    }


    public class TfsQuery : ITfsQuery
    {
        internal TfsQuery(WorkItemStore workItemStore, Project p)
        {
            _workItemStore = workItemStore;
            _project = p;
        }

        private WorkItemStore _workItemStore;
        public WorkItemStore WorkItemStore
        {
            get { return _workItemStore; }
        }

        private Project _project;
        public Project Project
        {
            get { return _project; }
        }

        public QueryDefinition QueryDefinition
        {
            get;
            set;
        }

        public ITfsQueryFolder Parent
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string QualifiedTitle
        {
            get
            {
                string result = Title;
                ITfsQueryFolder current = Parent;
                while (current != null)
                {
                    result = current.Title + "/" + result;
                    current = (current as TfsQueryFolder).Parent;
                }
                return result;
            }
        }

        public int Level
        {
            get;
            set;
        }

        public IEnumerable<ITfsWorkItem> GetResults()
        {
            var variables = new Dictionary<string, string>();
            variables["project"] = Project.Name;
            var query = new Query(WorkItemStore, QueryDefinition.QueryText, variables);
            if (query.IsLinkQuery)
            {
                var sourceIds = String.Join(", ", query.RunLinkQuery().Select(link => (link.SourceId != 0) ? link.SourceId : link.TargetId));
                foreach (WorkItem item in _workItemStore.Query(
                    "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.Description], [System.State] FROM WorkItems WHERE [System.Id] IN (" + sourceIds + ") ORDER BY [System.Id]", variables))
                {
                    yield return new TfsWorkItem(item);
                }
            }
            else
            {
                foreach (WorkItem item in _workItemStore.Query(
                    query.QueryString, variables))
                {
                    yield return new TfsWorkItem(item);
                }
            }
        }
    }
}
