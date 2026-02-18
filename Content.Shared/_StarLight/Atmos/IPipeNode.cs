using Content.Shared.Atmos.Components;
using Content.Shared.Atmos;

namespace Content.Shared._StarLight.Atmos;

public interface IPipeNode
{
    PipeDirection Direction { get; }
    AtmosPipeLayer Layer { get; }
}
