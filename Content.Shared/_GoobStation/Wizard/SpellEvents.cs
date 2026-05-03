using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Wizard;

public sealed partial class PolymorphSpellEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype>? ProtoId;

    [DataField]
    public bool MakeWizard = true;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool LoadActions;
}
