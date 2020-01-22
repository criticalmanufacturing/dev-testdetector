using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Changes
{
    /// <summary>
    /// Block Change interface.
    /// Describes a change block in a given file.
    /// </summary>
    public interface IBlockChange
    {
        /// <summary>
        /// Gets or sets the line.
        /// </summary>
        /// <value>
        /// The line.
        /// </value>
        int Line
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the start text of the method/class changed.
        /// </summary>
        /// <value>
        /// The start text.
        /// </value>
        string StartText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the name of the method changed.
        /// </summary>
        /// <value>
        /// The name of the method.
        /// </value>
        string MethodName
        {
            get;
        }
    }
}
