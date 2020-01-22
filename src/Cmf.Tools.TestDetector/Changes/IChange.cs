using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Changes
{
    /// <summary>
    /// Describes a change (regardless if it a file or a block)
    /// </summary>
    public interface IChange
    {
        /// <summary>
        /// Gets or sets the kind of the change.
        /// </summary>
        /// <value>
        /// The kind of the change.
        /// </value>
        Operation ChangeKind
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Change Operation enumeration
    /// </summary>
    public enum Operation
    {
        Added,
        Removed,
        Renamed,
        Modified
    }
}
