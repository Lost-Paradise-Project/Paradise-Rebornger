using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Offer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedOfferItemSystem))]
public sealed partial class OfferGiverComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsOffering = false;

    [DataField, AutoNetworkedField]
    public EntityUid? Item;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public float MaxOfferDistance = 2f;
}
