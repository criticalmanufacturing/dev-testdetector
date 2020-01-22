using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Changes
{
    public interface IBlockChangeFactory
    {
        IBlockChange Create();
    }
}
