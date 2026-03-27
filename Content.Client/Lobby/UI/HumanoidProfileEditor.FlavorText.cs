using Content.Client._ADT.CharecterFlavor;
using Content.Shared._ADT.CharecterFlavor;
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
            TabContainer.AddChild(_flavorText);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-flavortext-tab"));
            _flavorTextEdit = _flavorText.CFlavorTextInput;

            //ADT-tweak-start
            _flavorText.OnOOCNotesChanged += OnOOCNotesChange;
            _flavorText.OnHeadshotUrlChanged += OnHeadshotUrlChange;
            _flavorText.OnPreviewRequested += OnFlavorPreviewRequested;
            //ADT-tweak-end

            _flavorText.OnFlavorTextChanged += OnFlavorTextChange;
        }
        else
        {
            if (_flavorText == null)
                return;

            TabContainer.RemoveChild(_flavorText);
            _flavorText.OnFlavorTextChanged -= OnFlavorTextChange;
            //ADT-tweak-start
            _flavorText.OnOOCNotesChanged -= OnOOCNotesChange;
            _flavorText.OnHeadshotUrlChanged -= OnHeadshotUrlChange;
            _flavorText.OnPreviewRequested -= OnFlavorPreviewRequested;
            //ADT-tweak-end
            _flavorText.Dispose();
            _flavorTextEdit?.Dispose();
            _flavorTextEdit = null;
            _flavorText = null;
        }
    }

    private void OnFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithFlavorText(content);
        SetDirty();
    }

    //ADT-tweak-start: ООС заметки и юрл
    private void OnOOCNotesChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithOOCNotes(content);
        SetDirty();
    }
    private void OnHeadshotUrlChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithHeadshotUrl(content);
        SetDirty();
    }


    private void OnFlavorPreviewRequested()
    {
        if (Profile is null)
            return;

        if (!_entManager.EntityExists(SpriteView.PreviewDummy))
            return;

        var flavor = _entManager.EnsureComponent<CharacterFlavorComponent>(SpriteView.PreviewDummy);
        flavor.FlavorText = Profile.FlavorText ?? string.Empty;
        flavor.OOCNotes = Profile.OOCNotes ?? string.Empty;
        flavor.HeadshotUrl = Profile.HeadshotUrl ?? string.Empty;

        var controller = UserInterfaceManager.GetUIController<CharacterFlavorUiController>();
        controller.OpenPreviewMenu(SpriteView.PreviewDummy);

        // Попросить сервер скачать и прислать картинку для предпросмотра хэдшота.
        if (!string.IsNullOrWhiteSpace(Profile.HeadshotUrl))
        {
            _entManager.System<CharecterFlavorSystem>().RequestHeadshotPreview(Profile.HeadshotUrl);
        }
    }
    //ADT-tweak-end

    private void UpdateFlavorTextEdit()
    {
        if (_flavorTextEdit != null)
        {
            _flavorTextEdit.TextRope = new Rope.Leaf(Profile?.FlavorText ?? "");
            // ADT-Tweak-start
            if (_flavorText == null)
                return;

            _flavorText.COOCTextInput.TextRope = new Rope.Leaf(Profile?.OOCNotes ?? "");
            _flavorText.CHeadshotUrlInput.Text = Profile?.HeadshotUrl ?? "";
            // ADT-Tweak-end
        }
    }
}
