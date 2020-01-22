using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Cmf.Tools.TestDetector.Changes
{
    public class DefaultBlockChange : IBlockChange
    {
        private static readonly Regex _signatureRegex = new Regex(@"^.*\s(?=(?<name>.*)\().*$");

        public int Line
        {
            get;
            set;
        }

        public string StartText
        {
            get;
            set;
        }

        public string MethodName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(StartText))
                {
                    return null;
                }

                var match = _signatureRegex.Match(StartText);
                if (!match.Success)
                {
                    return null;
                }

                var nameMatch = match.Groups["name"];
                if (!nameMatch.Success)
                {
                    return null;
                }

                return nameMatch.Value;
            }
        }
    }
}
