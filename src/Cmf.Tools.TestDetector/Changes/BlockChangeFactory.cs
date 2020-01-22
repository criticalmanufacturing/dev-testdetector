using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Changes
{
    /// <summary>
    /// Block Change Factory implementation
    /// </summary>
    /// <seealso cref="Cmf.Tools.TestDetector.Changes.IBlockChangeFactory" />
    public class BlockChangeFactory : IBlockChangeFactory
    {
        private ILifetimeScope _scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockChangeFactory"/> class.
        /// </summary>
        public BlockChangeFactory(ILifetimeScope scope)
        {
            _scope = scope;
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns>A block change method.</returns>
        public IBlockChange Create()
        {
            return _scope.Resolve<IBlockChange>();
        }
    }
}
