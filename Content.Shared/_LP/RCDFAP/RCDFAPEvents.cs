using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._LP.RCDFAP;

[Serializable, NetSerializable]
public sealed class RCDFAPSystemMessage(ProtoId<RCDFAPPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<RCDFAPPrototype> ProtoId = protoId;
}

[Serializable, NetSerializable]
public sealed class RCDFAPConstructionGhostRotationEvent(NetEntity netEntity, Direction direction) : EntityEventArgs
{
    public readonly NetEntity NetEntity = netEntity;
    public readonly Direction Direction = direction;
}

[Serializable, NetSerializable]
public enum RcdfapUiKey : byte
{
    Key
}
