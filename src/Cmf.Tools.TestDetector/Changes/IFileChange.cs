using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Changes
{
    /// <summary>
    /// Describes a file change
    /// </summary>
    /// <seealso cref="Cmf.Tools.TestDetector.Changes.IChange" />
    public interface IFileChanges : IChange
    {
        /// <summary>
        /// Gets or sets the relative path of the change. Relative to the repository root.
        /// </summary>
        /// <value>
        /// The relative path.
        /// </value>
        string RelativePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the absolute path of the file changed.
        /// </summary>
        /// <value>
        /// The absolute path.
        /// </value>
        string AbsolutePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the name of the file changed.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the extension of the file changed.
        /// </summary>
        /// <value>
        /// The extension.
        /// </value>
        string Extension
        {
            get;
        }

        /// <summary>
        /// Gets the blocks changed within the file.
        /// </summary>
        /// <value>
        /// The blocks.
        /// </value>
        IList<IBlockChange> Blocks
        {
            get;
        }
    }
}
