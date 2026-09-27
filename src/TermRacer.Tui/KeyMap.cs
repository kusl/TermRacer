using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TermRacer.Core;

namespace TermRacer.Tui;

internal static class KeyMap
{
    public static GameKey? Resolve(Key key) => key.NoShift.KeyCode switch
    {
        KeyCode.CursorUp or KeyCode.W => GameKey.Up,
        KeyCode.CursorDown or KeyCode.S => GameKey.Down,
        KeyCode.CursorLeft or KeyCode.A => GameKey.Left,
        KeyCode.CursorRight or KeyCode.D => GameKey.Right,
        KeyCode.Enter or KeyCode.Space => GameKey.Confirm,
        KeyCode.Tab => GameKey.ToggleAutopilot,
        KeyCode.Esc => GameKey.Exit,
        _ => null,
    };
}
