using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Repository
{
    /// <summary>
    /// Actual implementation of a Repository Factory.
    /// </summary>
    /// <seealso cref="Cmf.Tools.TestDetector.Repository.IRepositoryFactory" />
    public class RepositoryFactory : IRepositoryFactory
    {
        private ILifetimeScope _scope;

        public RepositoryFactory(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public IRepository Create(string path)
        {
            return _scope.Resolve<IRepository>(
                new NamedParameter("path", path)
            );
        }
    }
}
