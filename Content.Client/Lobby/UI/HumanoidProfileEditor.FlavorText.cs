using System.Threading;
using Content.Client._ADT.CharecterFlavor;
using Content.Shared._ADT.CharecterFlavor;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

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
    private CancellationTokenSource? _headshotRequestCts;
    private string? _lastHeadshotUrl;

    private async void OnHeadshotUrlChange(string content)
    {
        if (Profile is null)
            return;

        var url = content.Trim();

        if (url == _lastHeadshotUrl)
            return;

        _lastHeadshotUrl = url;

        Profile = Profile.WithHeadshotUrl(url);
        SetDirty();

        // Отменяем предыдущий запрос
        _headshotRequestCts?.Cancel();
        _headshotRequestCts?.Dispose();
        _headshotRequestCts = null;

        _headshotRequestCts = new CancellationTokenSource();

        var cts = _headshotRequestCts.Token;

        try
        {
            // Debounce: ждём 500мс перед запросом
            await Timer.Delay(500, cts);
            OnHeadshotPreviewRequestedDelayed(url);
        }
        catch (OperationCanceledException)
        {
            // Запрос отменён — это нормально
        }
    }

    private void OnHeadshotPreviewRequestedDelayed(string url)
    {
        if (Profile is null)
            return;

        if (!_entManager.EntityExists(SpriteView.PreviewDummy))
            return;

        var flavor = _entManager.EnsureComponent<CharacterFlavorComponent>(SpriteView.PreviewDummy);
        flavor.FlavorText = Profile.FlavorText ?? string.Empty;
        flavor.HeadshotUrl = url;

        // var controller = UserInterfaceManager.GetUIController<CharacterFlavorUiController>();
        // controller.OpenPreviewMenu(SpriteView.PreviewDummy);

        // Попросить сервер скачать и прислать картинку для предпросмотра хэдшота.
        if (!string.IsNullOrWhiteSpace(url))
        {
            _entManager.System<CharecterFlavorSystem>().RequestHeadshotPreview(url);
        }
    }

    private void OnFlavorPreviewRequested()
    {
        if (Profile is null)
            return;

        if (!_entManager.EntityExists(SpriteView.PreviewDummy))
            return;

        var flavor = _entManager.EnsureComponent<CharacterFlavorComponent>(SpriteView.PreviewDummy);
        flavor.FlavorText = Profile.FlavorText ?? string.Empty;
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
