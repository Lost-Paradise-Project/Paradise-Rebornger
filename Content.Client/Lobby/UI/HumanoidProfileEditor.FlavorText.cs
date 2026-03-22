using Content.Shared._FunkyStation.Records;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _allowFlavorText;

    private FlavorText.FlavorText? _flavorText;
    private TextEdit? _flavorTextEdit;

    /// <summary>
    /// Refreshes the flavor text editor status.
    /// </summary>
    public void RefreshFlavorText()
    {
        if (_allowFlavorText)
        {
            if (_flavorText != null)
                return;

            _flavorText = new FlavorText.FlavorText();
            // begin funky - flavor text is in the Records tab now
            _recordsTab.PersonalInfoContainer.Visible = true;
            _recordsTab.PersonalInfoContainer.AddChild(_flavorText);
            // end funky
            TabContainer.AddChild(_flavorText);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-flavortext-tab"));
            _flavorTextEdit = _flavorText.CFlavorTextInput;

            _flavorText.OnFlavorTextChanged += OnFlavorTextChange;
        }
        else
        {
            if (_flavorText == null)
                return;

            // begin funky - flavor text is in the Records tab now
            _recordsTab.PersonalInfoContainer.Visible = false;
            _recordsTab.PersonalInfoContainer.RemoveChild(_flavorText);
            // end funky
            TabContainer.RemoveChild(_flavorText);
            _flavorText.OnFlavorTextChanged -= OnFlavorTextChange;
            _flavorText.Dispose();
            _flavorTextEdit?.Dispose();
            _flavorTextEdit = null;
            _flavorText = null;
        }
    }

    // Start CD - Character Records
    private void UpdateProfileRecords(PlayerProvidedCharacterRecords records)
    {
        if (Profile is null)
            return;
        Profile = Profile.WithCDCharacterRecords(records);
        IsDirty = true;
    }
    // End CD - Character Records

    private void OnFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithFlavorText(content);
        SetDirty();
    }

    private void UpdateFlavorTextEdit()
    {
        if (_flavorTextEdit != null)
        {
            _flavorTextEdit.TextRope = new Rope.Leaf(Profile?.FlavorText ?? "");
        }
    }
}
