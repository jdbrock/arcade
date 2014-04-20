using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arcade
{
    public class Game
    {
        public String DisplayName { get; private set; }
        public String FullPath { get; private set; }

        public Game(String inDisplayName, String inFullPath)
        {
            DisplayName = inDisplayName;
            FullPath = inFullPath;
        }
    }
}
