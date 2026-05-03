using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Client._LP.Clothing;

namespace Content.Client._LP.Overlayes;

public sealed class NightVisionOverlay : Overlay
{
    private readonly IPrototypeManager _prototypeManager;
    private readonly NightVisionSystem _nvSystem;
    private static readonly ProtoId<ShaderPrototype> Shader = "LPPNightVision";
    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _shader;

    public NightVisionOverlay(NightVisionSystem nightVisionSystem)
    {
        IoCManager.InjectDependencies(this);
        _nvSystem = nightVisionSystem;
        _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        _shader = _prototypeManager.Index(Shader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_nvSystem.IsEnabled())
            return;

        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var nightcomp = _nvSystem.GetNightComp();

        if (nightcomp is null)
        {
            Logger.Error("NightVision Overlay: no component");
            return;
        }

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tint", nightcomp.Tint);
        _shader.SetParameter("luminance_threshold", nightcomp.Strength);
        _shader.SetParameter("noise_amount", nightcomp.Noise);
        _shader.SetParameter("scanline_intensity", nightcomp.Scanline);
        _shader.SetParameter("vignette_strength", nightcomp.Vignette);
        _shader.SetParameter("flicker_speed", nightcomp.Flicker);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
