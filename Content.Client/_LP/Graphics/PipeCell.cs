using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._LP.Graphics;

/// <summary>
/// A clickable pipe segment for the minigames hack, same click-handling
/// approach as SciFiButton (KeyBindDown + MouseFilter.Stop) but drawn with
/// StyleBoxPipe instead of a flat color.
/// </summary>
public sealed class PipeCell : PanelContainer
{
    public event Action? OnPressed;

    private readonly StyleBoxPipe _box = new();
    private bool _interactive = true;

    public PipeCell()
    {
        MouseFilter = MouseFilterMode.Stop;
        PanelOverride = _box;
    }

    public void SetVisual(int connectionMask, Color pipeColor, Color background, bool interactive)
    {
        _box.ConnectionMask = connectionMask;
        _box.PipeColor = pipeColor;
        _box.BackgroundColor = background;
        _interactive = interactive;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (!_interactive || args.Function != EngineKeyFunctions.UIClick)
            return;

        args.Handle();
        OnPressed?.Invoke();
    }
}
