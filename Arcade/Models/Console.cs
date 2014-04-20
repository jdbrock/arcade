using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcade
{
    public class Console
    {
        public String DisplayName { get; private set; }
        public String FullPath { get; private set; }

        public Console(String inDisplayName, String inFullPath)
        {
            DisplayName = inDisplayName;
            FullPath = inFullPath;
        }
    }
}
