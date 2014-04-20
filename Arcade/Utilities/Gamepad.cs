using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace Arcade
{
    public static class Gamepad
    {
        private const Int32 REPEAT_RATE = 150;

        private static UserIndex[] _activeControllers = new[] { UserIndex.One, UserIndex.Two, UserIndex.Three, UserIndex.Four };

        private static Task _statePoller;

        private static Dictionary<UserIndex, State> _lastState;
        private static Dictionary<UserIndex, Dictionary<GamepadButtonFlags, DateTime>> _lastDown;

        private static Dispatcher _dispatcher;

        private static event EventHandler<GamepadEventArgs> Changed;

        static Gamepad()
        {
            _statePoller = Task.Factory.StartNew((Action)StatePoll);
            _lastDown = new Dictionary<UserIndex, Dictionary<GamepadButtonFlags, DateTime>>();
            _lastState = new Dictionary<UserIndex, State>();
        }

        public static void SetDispatcher(Dispatcher inDispatcher)
        {
            _dispatcher = inDispatcher;
        }

        private static void StatePoll()
        {
            // Poll forever (each controller in turn.)
            while (true)
            {
                if (_dispatcher == null)
                    continue;

                foreach (var activeController in _activeControllers)
                {
                    // Get the controller.
                    var controller = new Controller(activeController);

                    // Bail if it isn't connected.
                    if (!controller.IsConnected)
                        continue;

                    // Get the current state.
                    var state = controller.GetState();

                    // Fire events for the diff between states.
                    FireEventsForStateChange(activeController, _lastState.ContainsKey(activeController) ? _lastState[activeController] : (State?)null, state);

                    // Store current state so we can diff it on the next loop.
                    _lastState[activeController] = state;
                }
            }
        }

        private static void FireEventsForStateChange(UserIndex inActiveController, State? inLastState, State inState)
        {
            var keysUp = new List<GamepadButtonFlags>();
            var keysDown = new List<GamepadButtonFlags>();
            var keysRepeated = new List<GamepadButtonFlags>();

            // Check each known key in turn.
            foreach (GamepadButtonFlags keyEnum in Enum.GetValues(typeof(GamepadButtonFlags)))
            {
                if (keyEnum == GamepadButtonFlags.None)
                    continue;

                var key = (Int32)keyEnum;

                // First state? Fire a key down.
                if (inLastState == null && ((((Int32)inState.Gamepad.Buttons) & key) == key))
                {
                    keysDown.Add((GamepadButtonFlags)key);
                    continue;
                }

                if (inLastState == null)
                    continue;

                // Key newly pressed, fire a key down.
                if ((((Int32)inLastState.Value.Gamepad.Buttons) & key) == 0 && ((((Int32)inState.Gamepad.Buttons) & key) == key))
                {
                    keysDown.Add((GamepadButtonFlags)key);
                    continue;
                }

                // Key no longer pressed, fire a key up.
                if ((((Int32)inLastState.Value.Gamepad.Buttons) & key) == key && ((((Int32)inState.Gamepad.Buttons) & key) == 0))
                {
                    keysUp.Add((GamepadButtonFlags)key);
                    continue;
                }

                // Key continues to be pressed, fire a key repeat.
                if ((((Int32)inLastState.Value.Gamepad.Buttons) & key) == key && ((((Int32)inState.Gamepad.Buttons) & key) == key))
                {
                    keysRepeated.Add((GamepadButtonFlags)key);
                    continue;
                }
            }

            // Fire changes (via dispatcher.)
            if (Changed != null)
            {
                if (!_lastDown.ContainsKey(inActiveController))
                    _lastDown.Add(inActiveController, new Dictionary<GamepadButtonFlags, DateTime>());

                // Fire key up events.
                foreach (var keyUp in keysUp)
                {
                    Changed(null, new GamepadEventArgs(keyUp, false));

                    if (_lastDown[inActiveController].ContainsKey(keyUp))
                        _lastDown[inActiveController].Remove(keyUp);
                }

                // Fire key down events.
                foreach (var keyDown in keysDown)
                {
                    Changed(null, new GamepadEventArgs(keyDown, true));
                    _lastDown[inActiveController][keyDown] = DateTime.UtcNow;
                }

                // Fire another key down for repeated keys (as per key repeat rate.)
                foreach (var keyRepeated in keysRepeated)
                    if (!_lastDown.ContainsKey(inActiveController) || !_lastDown[inActiveController].ContainsKey(keyRepeated) ||
                        _lastDown[inActiveController][keyRepeated] < DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(REPEAT_RATE)))
                    {
                        Changed(null, new GamepadEventArgs(keyRepeated, true));
                        _lastDown[inActiveController][keyRepeated] = DateTime.UtcNow;
                    }
            }
        }

        private static void Dispatch(Action inAction)
        {
            _dispatcher.BeginInvoke(inAction);
        }

        public static void RegisterWithHandler(Action<GamepadEventArgs> inButtonHandler)
        {
            Changed += (S, E) => inButtonHandler(E);
        }

        public static void DeregisterWithHandler(Action<GamepadEventArgs> inButtonHandler)
        {
            Changed -= (S, E) => inButtonHandler(E);
        }

        public static void RegisterAsKeys()
        {
            Changed += (S, E) =>
            {
                if (App.CurrentlyRunningEmulator != null && E.Button != GamepadButtonFlags.Back)
                    return;

                switch (E.Button)
                {
                    case GamepadButtonFlags.DPadUp:
                        Dispatch(() => SendKeys(Key.Up, E.IsDown));
                        break;

                    case GamepadButtonFlags.DPadDown:
                        Dispatch(() => SendKeys(Key.Down, E.IsDown));
                        break;

                    case GamepadButtonFlags.DPadLeft:
                        Dispatch(() => SendKeys(Key.Left, E.IsDown));
                        break;

                    case GamepadButtonFlags.DPadRight:
                        Dispatch(() => SendKeys(Key.Right, E.IsDown));
                        break;

                    case GamepadButtonFlags.A:
                        Dispatch(() => SendKeys(Key.Enter, E.IsDown));
                        break;

                    case GamepadButtonFlags.X:
                        Dispatch(() => SendKeys(Key.Back, E.IsDown));
                        break;

                    case GamepadButtonFlags.Back:
                        if (App.CurrentlyRunningEmulator != null)
                            App.CurrentlyRunningEmulator.CloseMainWindow();
                        break;

                    case GamepadButtonFlags.RightShoulder:
                        Dispatch(() => SendKeys(Key.PageDown, true));
                        break;

                    case GamepadButtonFlags.LeftShoulder:
                        Dispatch(() => SendKeys(Key.PageUp, true));
                        break;
                }
            };
        }

        /// <summary>
        /// Taken from:
        ///     http://stackoverflow.com/questions/11572411/sendkeys-send-method-in-wpf-application
        /// </summary>
        private static void SendKeys(Key inKey, Boolean inIsDown)
        {
            if (Keyboard.PrimaryDevice != null)
                if (Keyboard.PrimaryDevice.ActiveSource != null)
                {
                    var e = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, inKey)
                    {
                        RoutedEvent = inIsDown ? Keyboard.KeyDownEvent : Keyboard.KeyUpEvent
                    };

                    InputManager.Current.ProcessInput(e);
                }
        }
    }
}
