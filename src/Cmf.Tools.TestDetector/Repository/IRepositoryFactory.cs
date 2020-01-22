using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Repository
{
    /// <summary>
    /// Represents a Repository Factory
    /// </summary>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// Creates a repository with the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>A repository representation</returns>
        IRepository Create(string path);
    }
}
