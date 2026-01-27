using Content.Shared.Construction.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Content.Shared._White.CustomGhostSystem;  //WWDP edit

namespace Content.Client.Lobby
{
    public interface IClientPreferencesManager
    {
        event Action OnServerDataLoaded;

        bool ServerDataLoaded => Settings != null;

        GameSettings? Settings { get; }
        PlayerPreferences? Preferences { get; }
        void Initialize();
        void SelectCharacter(ICharacterProfile profile);
        void SelectCharacter(int slot);
        void UpdateCharacter(ICharacterProfile profile, int slot);
        void CreateCharacter(ICharacterProfile profile);
        void DeleteCharacter(ICharacterProfile profile);
        void DeleteCharacter(int slot);
        void UpdateConstructionFavorites(List<ProtoId<ConstructionPrototype>> favorites);
        void SetCustomGhost(ProtoId<CustomGhostPrototype> ghostProto); // WWDP EDIT
    }
}
