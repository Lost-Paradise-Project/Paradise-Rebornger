using Content.Shared._GoobStation.GPS;
using Content.Shared._GoobStation.GPS.Components;

namespace Content.Client._GoobStation.GPS;

public sealed class GpsSystem : SharedGpsSystem
{
    protected override void UpdateUi(Entity<GPSComponent> ent)
    {
        if (UiSystem.TryGetOpenUi<GpsBoundUserInterface>(ent.Owner,
                GpsUiKey.Key,
                out var bui))
            bui.UpdateWindow();
    }
}
