using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._LP.Graphics;

/// <summary>
/// A flat panel with corner "targeting bracket" accents instead of a plain border
/// reads as a HUD/terminal frame rather than a generic window. Deliberately only uses
/// DrawRect (no lines/polygons).
/// </summary>
public sealed class StyleBoxSciFi : StyleBox
{
    public Color BackgroundColor { get; set; }
    public Color AccentColor { get; set; }
    public Color EdgeColor { get; set; }
    public new float Padding { get; set; } = 10f;
    public float BracketSize { get; set; } = 16f;
    public float BracketThickness { get; set; } = 2f;

    /// <summary>
    /// Draws a faint grid of hairlines across the background when set, like the
    /// dot/graph paper backing on a lot of sci-fi HUDs. Off by default since it's
    /// only worth the extra draw calls on the bigger panels.
    /// </summary>
    public bool ShowGrid { get; set; }
    public float GridSpacing { get; set; } = 20f;

    /// <summary>
    /// 0..1 fraction of the panel's height, or -1 to turn the sweep off. Meant to be
    /// nudged along slowly and continuously each frame, a fast or oscillating value
    /// here reads as a glitch, not ambiance.
    /// </summary>
    public float ScanlinePosition { get; set; } = -1f;

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        handle.DrawRect(box, BackgroundColor);

        if (ShowGrid)
            DrawGrid(handle, box, uiScale);

        handle.DrawRect(new UIBox2(box.Left, box.Top, box.Right, box.Top + 1), EdgeColor);
        handle.DrawRect(new UIBox2(box.Left, box.Bottom - 1, box.Right, box.Bottom), EdgeColor);
        handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + 1, box.Bottom), EdgeColor);
        handle.DrawRect(new UIBox2(box.Right - 1, box.Top, box.Right, box.Bottom), EdgeColor);

        if (ScanlinePosition is >= 0f and <= 1f)
        {
            var y = box.Top + ScanlinePosition * (box.Bottom - box.Top);
            DrawScanline(handle, box, y);
        }

        var size = BracketSize * uiScale;
        var thick = BracketThickness * uiScale;

        DrawBracket(handle, box.Left, box.Top, size, thick, inX: 1, inY: 1);
        DrawBracket(handle, box.Right, box.Top, size, thick, inX: -1, inY: 1);
        DrawBracket(handle, box.Left, box.Bottom, size, thick, inX: 1, inY: -1);
        DrawBracket(handle, box.Right, box.Bottom, size, thick, inX: -1, inY: -1);
    }

    private void DrawGrid(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var spacing = GridSpacing * uiScale;
        if (spacing <= 1f)
            return;

        var lineColor = new Color(AccentColor.R, AccentColor.G, AccentColor.B, 0.06f);

        for (var x = box.Left + spacing; x < box.Right; x += spacing)
            handle.DrawRect(new UIBox2(x, box.Top, x + 1, box.Bottom), lineColor);

        for (var y = box.Top + spacing; y < box.Bottom; y += spacing)
            handle.DrawRect(new UIBox2(box.Left, y, box.Right, y + 1), lineColor);
    }

    // Three stacked, widening, fading bands instead of one hard line, reads as a
    // soft glow passing through rather than a line snapping to a new spot each frame.
    private void DrawScanline(DrawingHandleScreen handle, UIBox2 box, float y)
    {
        DrawBand(handle, box, y - 3f, y + 3f, 0.06f);
        DrawBand(handle, box, y - 1.5f, y + 1.5f, 0.14f);
        DrawBand(handle, box, y - 0.5f, y + 0.5f, 0.25f);
    }

    private void DrawBand(DrawingHandleScreen handle, UIBox2 box, float y0, float y1, float alpha)
    {
        var top = Math.Max(box.Top, y0);
        var bottom = Math.Min(box.Bottom, y1);
        if (bottom <= top)
            return;

        handle.DrawRect(new UIBox2(box.Left, top, box.Right, bottom), new Color(AccentColor.R, AccentColor.G, AccentColor.B, alpha));
    }

    // Draws an L-shaped bracket at one corner, its two arms pointing inward
    // (inX/inY = +1 or -1) toward the center of the panel.
    private void DrawBracket(DrawingHandleScreen handle, float cx, float cy, float size, float thick, float inX, float inY)
    {
        var armEndX = cx + size * inX;
        var armEndY = cy + size * inY;
        var thickEndX = cx + thick * inX;
        var thickEndY = cy + thick * inY;

        handle.DrawRect(MakeBox(cx, cy, armEndX, thickEndY), AccentColor);
        handle.DrawRect(MakeBox(cx, cy, thickEndX, armEndY), AccentColor);
    }

    private static UIBox2 MakeBox(float x0, float y0, float x1, float y1) =>
        new(Math.Min(x0, x1), Math.Min(y0, y1), Math.Max(x0, x1), Math.Max(y0, y1));

    protected override float GetDefaultContentMargin(Margin margin) => Padding;
}
