using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void SetName(string newName)
    {
        Profile = Profile?.WithName(newName);
        SetDirty();

        if (!IsDirty)
            return;

        SpriteView.SetName(newName);
    }

    // Goob Station - Start
    private void SetProfileHeight(float height)
    {
        Profile = Profile?.WithHeight(height);
        ReloadProfilePreview();
        IsDirty = true;
    }

    private void SetProfileWidth(float width)
    {
        Profile = Profile?.WithWidth(width);
        ReloadProfilePreview();
        IsDirty = true;
    }
    // Goob Station - End

    private void UpdateNameEdit()
    {
        NameEdit.Text = Profile?.Name ?? "";
    }

    private void RandomizeEverything()
    {
        Profile = HumanoidCharacterProfile.Random();
        SetProfile(Profile, CharacterSlot);
        SetDirty();
    }

    private void RandomizeName()
    {
        if (Profile == null) return;
        var name = HumanoidCharacterProfile.GetName(Profile.Species, Profile.Gender);
        SetName(name);
        UpdateNameEdit();

        _recordsTab.Update(Profile); // CD - Character Records
    }
}
