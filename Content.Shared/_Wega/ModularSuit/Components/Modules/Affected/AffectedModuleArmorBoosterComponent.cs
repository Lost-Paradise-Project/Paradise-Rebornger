using Content.Shared.Damage;

namespace Content.Shared._Wega.ModularSuit;

[RegisterComponent]
public sealed partial class AffectedModuleArmorBoosterComponent : Component
{
    [DataField(required: true)]
    public DamageModifierSet Modifiers = new();
}
