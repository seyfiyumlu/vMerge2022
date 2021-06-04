using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;
using alexbegh.vMerge.Model.Interfaces;

namespace alexbegh.vMerge.Model.Implementation
{
    public class TfsMergeConflict : ITfsMergeConflict
    {
        public string ServerPath
        {
            get;
            private set;
        }

        public string Message
        {
            get;
            private set;
        }

        public Conflict Conflict
        {
            get;
            private set;
        }

        internal TfsMergeConflict(Conflict source)
        {
            ServerPath = source.ServerPath;
            Message = source.GetFullMessage().Replace(source.LocalPath, source.ServerPath);

            Conflict = source;
        }
    }
}
