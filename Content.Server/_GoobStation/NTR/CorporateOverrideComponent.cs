using Robust.Shared.Containers;

namespace Content.Server._GoobStation.NTR
{
    [RegisterComponent]
    public sealed partial class CorporateOverrideComponent : Component
    {
        [DataField]
        public string UnlockedCategory = "NTREvil";

        public ContainerSlot OverrideSlot = default!;
        public const string ContainerId = "CorporateOverrideSlot";
}
}
