using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._LP.Graphics;

/// <summary>
/// A clickable panel built entirely from our own primitives instead of the engine's
/// stock Button - so its look doesn't depend on whatever button stylesheet this
/// fork ships at all.
/// </summary>
public sealed class SciFiButton : PanelContainer
{
    public event Action? OnPressed;

    private readonly Label _label = new();
    private readonly StyleBoxFlat _box = new();
    private bool _disabled;

    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            UpdateVisual();
        }
    }

    public string Text
    {
        get => _label.Text ?? string.Empty;
        set => _label.Text = value;
    }

    public Color NormalColor { get; set; } = Color.FromHex("#1c2430");
    public Color TextColor { get; set; } = Color.White;

    public SciFiButton()
    {
        // PanelContainer doesn't grab mouse input by default (it's normally just a
        // decorative background) - without this, clicks pass straight through and
        // KeyBindDown below never fires at all.
        MouseFilter = MouseFilterMode.Stop;
        _label.MouseFilter = MouseFilterMode.Ignore;

        PanelOverride = _box;
        AddChild(_label);
        UpdateVisual();
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (_disabled || args.Function != EngineKeyFunctions.UIClick)
            return;

        args.Handle();
        OnPressed?.Invoke();
    }

    public void UpdateVisual()
    {
        _box.BackgroundColor = _disabled ? Dim(NormalColor) : NormalColor;
        _label.FontColorOverride = _disabled ? Dim(TextColor) : TextColor;
    }

    private static Color Dim(Color c) => new(c.R * 0.4f, c.G * 0.4f, c.B * 0.4f, c.A);
}
