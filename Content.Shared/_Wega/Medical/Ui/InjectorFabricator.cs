using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Medical.Ui;

[Serializable, NetSerializable]
public enum InjectorFabricatorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class InjectorFabricatorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool IsProducing;
    public readonly bool CanProduce;
    public readonly NetEntity? Beaker;
    public readonly ContainerInfo? BeakerContainerInfo;
    public readonly Solution? BufferSolution;
    public readonly FixedPoint2 BufferVolume;
    public readonly FixedPoint2 BufferMaxVolume;
    public readonly Dictionary<ReagentId, FixedPoint2>? Recipe;
    public readonly string? CustomName;
    public readonly int InjectorsToProduce;
    public readonly int InjectorsProduced;

    public InjectorFabricatorBoundUserInterfaceState(
        bool isProducing,
        bool canProduce,
        NetEntity? beaker,
        ContainerInfo? beakerContainerInfo,
        Solution? bufferSolution,
        FixedPoint2 bufferVolume,
        FixedPoint2 bufferMaxVolume,
        Dictionary<ReagentId, FixedPoint2>? recipe,
        string? customName,
        int injectorsToProduce,
        int injectorsProduced)
    {
        IsProducing = isProducing;
        CanProduce = canProduce;
        Beaker = beaker;
        BeakerContainerInfo = beakerContainerInfo;
        BufferSolution = bufferSolution;
        BufferVolume = bufferVolume;
        BufferMaxVolume = bufferMaxVolume;
        Recipe = recipe;
        CustomName = customName;
        InjectorsToProduce = injectorsToProduce;
        InjectorsProduced = injectorsProduced;

    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabricatorTransferBufferToBeakerMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;
    public readonly FixedPoint2 Amount;

    public InjectorFabricatorTransferBufferToBeakerMessage(ReagentId reagentId, FixedPoint2 amount)
    {
        ReagentId = reagentId;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabricatorTransferBeakerToBufferMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;
    public readonly FixedPoint2 Amount;

    public InjectorFabricatorTransferBeakerToBufferMessage(ReagentId reagentId, FixedPoint2 amount)
    {
        ReagentId = reagentId;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabricatorSetReagentMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;
    public readonly FixedPoint2 Amount;

    public InjectorFabricatorSetReagentMessage(ReagentId reagentId, FixedPoint2 amount)
    {
        ReagentId = reagentId;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabricatorRemoveReagentMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;

    public InjectorFabricatorRemoveReagentMessage(ReagentId reagentId)
    {
        ReagentId = reagentId;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabricatorProduceMessage : BoundUserInterfaceMessage
{
    public readonly int Amount;
    public readonly string? CustomName;

    public InjectorFabricatorProduceMessage(int amount, string? customName)
    {
        Amount = amount;
        CustomName = customName;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabricatorEjectMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class InjectorFabricatorSyncRecipeMessage : BoundUserInterfaceMessage
{
    public readonly Dictionary<ReagentId, FixedPoint2>? Recipe;

    public InjectorFabricatorSyncRecipeMessage(Dictionary<ReagentId, FixedPoint2>? recipe)
    {
        Recipe = recipe;
    }
}
