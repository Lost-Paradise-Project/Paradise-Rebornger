using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared._LP.RCDFAP;
using Content.Shared._LP.RCDFAP.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._LP.RCDFAP;

[UsedImplicitly]
public sealed class RCDFAPMenuBoundUserInterface : BoundUserInterface
{
    private const string TopLevelActionCategory = "Main";

    private static readonly Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)> PrototypesGroupingInfo
        = new Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)>
        {
            ["DevicesAndAtmos"] = ("rcdfap-component-devices-and-atmos", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/Devices.png"))),
            ["GaspipesMain"] = ("rcdfap-component-gaspipes-main", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/GaspipesMain.png"))),
            ["GaspipesAlt1"] = ("rcdfap-component-gaspipes-alt1", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/GaspipesRight.png"))),
            ["GaspipesAlt2"] = ("rcdfap-component-gaspipes-alt2", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/GaspipesLeft.png"))),
            ["DisposalPipe"] = ("rcdfap-component-disposalpipe", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/DisposalPipe.png"))),
            ["Tiles"] = ("rcdfap-component-tiles", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/Tiles.png"))),
            ["Airlocks"] = ("rcdfap-component-airlocks", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/Airlocks.png"))),
            ["Windows"] = ("rcdfap-component-windows", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/Windows.png"))),
            ["Cables"] = ("rcdfap-component-cables", new SpriteSpecifier.Texture(new ResPath("/Textures/_LP/Interface/Radial/RCDFAP/Category/Cables.png"))),
        };

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private SimpleRadialMenu? _menu;

    public RCDFAPMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<RCDFAPComponent>(Owner, out var rcdfap))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var models = ConvertToButtons(rcdfap.AvailablePrototypes);
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(HashSet<ProtoId<RCDFAPPrototype>> prototypes)
    {
        Dictionary<string, List<RadialMenuActionOptionBase>> buttonsByCategory = new();
        ValueList<RadialMenuActionOptionBase> topLevelActions = new();
        foreach (var protoId in prototypes)
        {
            var prototype = _prototypeManager.Index(protoId);
            if (prototype.Category == TopLevelActionCategory)
            {
                var topLevelActionOption = new RadialMenuActionOption<RCDFAPPrototype>(HandleMenuOptionClick, prototype)
                {
                    IconSpecifier = RadialMenuIconSpecifier.With(prototype.Sprite),
                    ToolTip = GetTooltip(prototype)
                };
                topLevelActions.Add(topLevelActionOption);
                continue;
            }

            if (!PrototypesGroupingInfo.TryGetValue(prototype.Category, out var groupInfo))
                continue;

            if (!buttonsByCategory.TryGetValue(prototype.Category, out var list))
            {
                list = new List<RadialMenuActionOptionBase>();
                buttonsByCategory.Add(prototype.Category, list);
            }

            var actionOption = new RadialMenuActionOption<RCDFAPPrototype>(HandleMenuOptionClick, prototype)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(prototype.Sprite),
                ToolTip = GetTooltip(prototype)
            };
            list.Add(actionOption);
        }

        var models = new RadialMenuOptionBase[buttonsByCategory.Count + topLevelActions.Count];
        var i = 0;
        foreach (var (key, list) in buttonsByCategory)
        {
            var groupInfo = PrototypesGroupingInfo[key];
            models[i] = new RadialMenuNestedLayerOption(list)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(groupInfo.Sprite),
                ToolTip = Loc.GetString(groupInfo.Tooltip)
            };
            i++;
        }

        foreach (var action in topLevelActions)
        {
            models[i] = action;
            i++;
        }

        return models;
    }

    private void HandleMenuOptionClick(RCDFAPPrototype proto)
    {
        // A predicted message cannot be used here as the RCD UI is closed immediately
        // after this message is sent, which will stop the server from receiving it
        SendMessage(new RCDFAPSystemMessage(proto.ID));


        if (_playerManager.LocalSession?.AttachedEntity == null)
            return;

        var msg = Loc.GetString("rcdfap-component-change-mode", ("mode", Loc.GetString(proto.SetName)));

        if (proto.Mode is RcdfapMode.ConstructTile or RcdfapMode.ConstructObject)
        {
            var name = Loc.GetString(proto.SetName);

            if (proto.Prototype != null &&
                _prototypeManager.TryIndex(proto.Prototype, out var entProto)) // don't use Resolve because this can be a tile
            {
                name = entProto.Name;
            }

            msg = Loc.GetString("rcdfap-component-change-build-mode", ("name", name));
        }

        // Popup message
        var popup = EntMan.System<PopupSystem>();
        popup.PopupClient(msg, Owner, _playerManager.LocalSession.AttachedEntity);
    }

    private string GetTooltip(RCDFAPPrototype proto)
    {
        string tooltip;

        if (proto.Mode is RcdfapMode.ConstructTile or RcdfapMode.ConstructObject
            && proto.Prototype != null
            && _prototypeManager.TryIndex(proto.Prototype, out var entProto)) // don't use Resolve because this can be a tile
        {
            tooltip = Loc.GetString(entProto.Name);
        }
        else
        {
            tooltip = Loc.GetString(proto.SetName);
        }

        if (string.IsNullOrWhiteSpace(tooltip))
        {
            Logger.Warning($"RCDFAP tooltip is empty for prototype {proto.ID}");
            return proto.ID;
        }


        tooltip = OopsConcat(char.ToUpper(tooltip[0]).ToString(), tooltip.Remove(0, 1));

        return tooltip;
    }

    private static string OopsConcat(string a, string b)
    {
        // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
        return a + b;
    }
}
