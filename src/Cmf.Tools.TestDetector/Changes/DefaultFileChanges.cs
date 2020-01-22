using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.Tools.TestDetector.Changes
{
    public class DefaultFileChanges : IFileChanges
    {
        private List<IBlockChange> _blocks = new List<IBlockChange>();

        public string RelativePath
        { 
            get;
            set;
        }

        public string AbsolutePath
        {
            get;
            set;
        }


        public string Name
        {
            get
            {
                return System.IO.Path.GetFileNameWithoutExtension(RelativePath);
            }
        }

        public string Extension
        {
            get
            {
                return System.IO.Path.GetExtension(RelativePath);
            }
        }

        public IList<IBlockChange> Blocks
        {
            get => _blocks;
        }

        public Operation ChangeKind
        {
            get;
            set;
        }

        public DefaultFileChanges()
        {

        }

    }
}
