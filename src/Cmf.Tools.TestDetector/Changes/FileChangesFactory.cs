using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Changes
{
    public class FileChangesFactory : IFileChangesFactory
    {
        private ILifetimeScope _scope;

        public FileChangesFactory(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public IFileChanges Create()
        {
            return _scope.Resolve<IFileChanges>();
        }
    }
}
