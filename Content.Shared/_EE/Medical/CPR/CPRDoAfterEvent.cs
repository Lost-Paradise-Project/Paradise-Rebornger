using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._EE.Medical.CPR;

[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : SimpleDoAfterEvent;
