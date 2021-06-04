using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Managers.Background
{
    /// <summary>
    /// Value type to contain progress information, to be used by TrackProgressImpl
    /// </summary>
    public struct ProgressData
    {
        /// <summary>
        /// The maximum progress
        /// </summary>
        public double MaxProgress;

        /// <summary>
        /// The current progress
        /// </summary>
        public double CurrentProgress;

        /// <summary>
        /// Progress info string
        /// </summary>
        public string ProgressInfo;
    }

}
