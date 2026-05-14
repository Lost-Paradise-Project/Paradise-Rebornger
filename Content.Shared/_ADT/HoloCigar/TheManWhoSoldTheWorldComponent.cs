using Robust.Shared.Audio;

namespace Content.Shared._ADT.HoloCigar.Components;

[RegisterComponent]
public sealed partial class TheManWhoSoldTheWorldComponent : Component
{
    [ViewVariables]
    public SoundSpecifier DeathAudio = new SoundPathSpecifier("/Audio/_ADT/Items/TheManWhoSoldTheWorld/death.ogg");
}
