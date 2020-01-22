using Cmf.Tools.TestDetector.Changes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Repository
{
    /// <summary>
    /// Repository Interface. Represents a source code repository.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IRepository : IDisposable
    {
        /// <summary>
        /// Gets the changes.
        /// </summary>
        /// <param name="sourceCommitId">The source commit identifier.</param>
        /// <param name="targetCommitId">The target commit identifier.</param>
        /// <param name="glob">The glob.</param>
        /// <returns></returns>
        IEnumerable<IFileChanges> GetChanges(string sourceCommitId, string targetCommitId, string glob = null);
    }
}
