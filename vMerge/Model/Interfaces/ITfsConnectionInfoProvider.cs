using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    interface ITfsConnectionInfoProvider : INotifyPropertyChanged
    {
        Uri Uri { get; }
        TeamProject Project { get; }
    }
}
