using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcade
{
    public class GamepadEventArgs : EventArgs
    {
        public GamepadButtonFlags Button { get; private set; }
        public Boolean IsDown { get; private set; }

        public GamepadEventArgs(GamepadButtonFlags inButton, Boolean inIsDown)
        {
            Button = inButton;
            IsDown = inIsDown;
        }
    }
}
