using Content.Shared._ERPModule.Data;
using Content.Shared._GoobStation.Barks;
using Content.Shared.Corvax.TTS;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Dictates what species and age this character "looks like"
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(HumanoidProfileSystem))]
public sealed partial class HumanoidProfileComponent : Component
{
    [DataField, AutoNetworkedField]
    public Gender Gender;

    // LP edit start
    [DataField, AutoNetworkedField]
    public ErpStatus ErpStatus;
    // LP edit end

    [DataField, AutoNetworkedField]
    public Sex Sex;

    [DataField, AutoNetworkedField]
    public int Age = 18;

    [DataField] // Goob Station - Barks
    public ProtoId<BarkPrototype> BarkVoice { get; set; } = HumanoidProfileSystem.DefaultBarkVoice; // Goob Station - Barks

    [DataField, AutoNetworkedField]
    public ProtoId<SpeciesPrototype> Species = HumanoidCharacterProfile.DefaultSpecies;

    // Corvax-TTS-Start
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype> Voice { get; set; } = HumanoidProfileSystem.DefaultVoice;
    // Corvax-TTS-End

    // begin Goobstation: port EE height/width sliders

    /// <summary>
    ///     The height of this humanoid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Height = 1f;

    /// <summary>
    ///     The width of this humanoid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Width = 1f;

    // end Goobstation: port EE height/width sliders
}
