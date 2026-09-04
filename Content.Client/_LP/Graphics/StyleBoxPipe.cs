using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._LP.Graphics;

/// <summary>
/// Draws a background square plus thick "spokes" from the center out to whichever
/// edges are connected (bit0=N, bit1=E, bit2=S, bit3=W), so a rotated pipe segment
/// actually looks like it's pointing somewhere different.
/// </summary>
public sealed class StyleBoxPipe : StyleBox
{
    public Color BackgroundColor { get; set; }
    public Color PipeColor { get; set; }
    public int ConnectionMask { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        handle.DrawRect(box, BackgroundColor);

        if (ConnectionMask == 0)
            return;

        var cx = (box.Left + box.Right) / 2f;
        var cy = (box.Top + box.Bottom) / 2f;
        var half = (box.Right - box.Left) * 0.14f;

        handle.DrawRect(new UIBox2(cx - half, cy - half, cx + half, cy + half), PipeColor);

        if ((ConnectionMask & 1) != 0)
            handle.DrawRect(new UIBox2(cx - half, box.Top, cx + half, cy + half), PipeColor);
        if ((ConnectionMask & 2) != 0)
            handle.DrawRect(new UIBox2(cx - half, cy - half, box.Right, cy + half), PipeColor);
        if ((ConnectionMask & 4) != 0)
            handle.DrawRect(new UIBox2(cx - half, cy - half, cx + half, box.Bottom), PipeColor);
        if ((ConnectionMask & 8) != 0)
            handle.DrawRect(new UIBox2(box.Left, cy - half, cx + half, cy + half), PipeColor);
    }

    protected override float GetDefaultContentMargin(Margin margin) => 0f;
}
