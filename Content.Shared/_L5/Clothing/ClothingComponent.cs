using Content.Shared.Inventory;

namespace Content.Shared.Clothing.Components;

public sealed partial class ClothingComponent
{
    [DataField]
    public SlotFlags PreferredSlots = SlotFlags.NONE;
}
