using Content.Shared.FixedPoint;

namespace Content.Shared._GoobStation.NTR.Events;

public sealed class NtrListingPurchaseEvent(FixedPoint2 cost)
{
    public FixedPoint2 Cost = cost;
}
