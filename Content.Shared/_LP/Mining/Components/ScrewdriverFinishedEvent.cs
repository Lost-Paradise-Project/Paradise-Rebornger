using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Mining.Components;

[Serializable, NetSerializable]
public sealed partial class ScrewdriverFinishedEvent : SimpleDoAfterEvent
{
}

