using System.Numerics;
using Content.Shared._EE.HeightAdjust;
using Content.Shared._GoobStation.Barks;
using Content.Shared.Corvax.TTS;
using Content.Shared.Examine;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

public sealed class HumanoidProfileSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;
    [Dependency] private readonly HeightAdjustSystem _heightAdjust = default!; // Goobstation: port EE height/width sliders

    // Corvax-TTS-Start
    public const string DefaultVoice = "nord";
    public static readonly Dictionary<Sex, string> DefaultSexVoice = new()
    {
        {Sex.Male, "nord"},
        {Sex.Female, "amina"},
        {Sex.Unsexed, "alyx"},
    };
    // Corvax-TTS-End
    public static readonly ProtoId<BarkPrototype> DefaultBarkVoice = "Alto"; // Goob Station - Barks

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, ExaminedEvent>(OnExamined);
    }

    // begin Goobstation: port EE height/width sliders

    /// <summary>
    ///     Set the height of a humanoid mob
    /// </summary>
    /// <param name="uid">The humanoid mob's UID</param>
    /// <param name="height">The height to set the mob to</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetHeight(EntityUid uid, float height, bool sync = true, HumanoidProfileComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid) || MathHelper.CloseTo(humanoid.Height, height, 0.001f))
            return;

        var species = _prototype.Index(humanoid.Species);
        humanoid.Height = Math.Clamp(height, species.MinHeight, species.MaxHeight);

        if (sync)
            Dirty(uid, humanoid);
    }

    /// <summary>
    ///     Set the width of a humanoid mob
    /// </summary>
    /// <param name="uid">The humanoid mob's UID</param>
    /// <param name="width">The width to set the mob to</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetWidth(EntityUid uid, float width, bool sync = true, HumanoidProfileComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid) || MathHelper.CloseTo(humanoid.Width, width, 0.001f))
            return;

        var species = _prototype.Index(humanoid.Species);
        humanoid.Width = Math.Clamp(width, species.MinWidth, species.MaxWidth);

        if (sync)
            Dirty(uid, humanoid);
    }

    /// <summary>
    ///     Set the scale of a humanoid mob
    /// </summary>
    /// <param name="uid">The humanoid mob's UID</param>
    /// <param name="scale">The scale to set the mob to</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetScale(EntityUid uid, Vector2 scale, bool sync = true, HumanoidProfileComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
            return;

        var species = _prototype.Index(humanoid.Species);
        humanoid.Height = Math.Clamp(scale.Y, species.MinHeight, species.MaxHeight);
        humanoid.Width = Math.Clamp(scale.X, species.MinWidth, species.MaxWidth);

        if (sync)
            Dirty(uid, humanoid);
    }

    // end Goobstation: port EE height/width sliders

    public void ApplyProfileTo(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.Species = profile.Species;
        ent.Comp.Sex = profile.Sex;
        // Corvax-TTS-start
        ent.Comp.Voice = profile.Voice;
        if (TryComp<TTSComponent>(ent, out var _TTSComponent) && _TTSComponent.VoicePrototypeId == null)
        {
            _TTSComponent.VoicePrototypeId = profile.Voice;
        }
        // Corvax-TTS-end
        // Goob Station - Barks start
        var ev = new ApplyBarkVoiceEvent(profile.BarkVoice);
        RaiseLocalEvent(ent, ref ev);
        // Goob Station - Barks end
        // begin Goobstation: port EE height/width sliders
        var species = _prototype.Index(profile.Species);

        if (profile.Height <= 0 || profile.Width <= 0)
            SetScale(ent, new Vector2(species.DefaultWidth, species.DefaultHeight), true, ent.Comp);
        else
            SetScale(ent, new Vector2(profile.Width, profile.Height), true, ent.Comp);

        _heightAdjust.SetScale(ent, new Vector2(profile.Width, profile.Height));
        // end Goobstation: port EE height/width sliders
        Dirty(ent);

        var sexChanged = new SexChangedEvent(ent.Comp.Sex, profile.Sex);
        RaiseLocalEvent(ent, ref sexChanged);

        if (TryComp<GrammarComponent>(ent, out var grammar))
        {
            _grammar.SetGender((ent, grammar), profile.Gender);
        }
    }

    private void OnExamined(Entity<HumanoidProfileComponent> ent, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(ent, EntityManager);
        var species = GetSpeciesRepresentation(ent.Comp.Species).ToLower();
        var age = GetAgeRepresentation(ent.Comp.Species, ent.Comp.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species)
    {
        if (_prototype.TryIndex(species, out var speciesPrototype))
            return Loc.GetString(speciesPrototype.Name);

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    /// <summary>
    /// Takes ID of the species prototype and an age, returns an approximate description
    /// </summary>
    public string GetAgeRepresentation(ProtoId<SpeciesPrototype> species, int age)
    {
        if (!_prototype.TryIndex(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
}

// LP edit start
[ByRefEvent]
public record struct ApplyBarkVoiceEvent(string BarkVoice);
// LP edit end
