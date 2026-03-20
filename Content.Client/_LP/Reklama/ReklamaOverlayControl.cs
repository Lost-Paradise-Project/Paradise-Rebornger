using System.Numerics;
using Content.Shared._LP.Reklama;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._LP.Reklama;

public static class ReklamaControlManager
{
    public static List<Control> GetAdIcons()
    {
        var protos = IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes<ReklamaPrototype>();
        var urlMan = IoCManager.Resolve<IUriOpener>();
        List<Control> reklama = [];

        foreach (var proto in protos)
        {
            string tooltip = proto.Name + "\n" + proto.Description;

            var btn = new Button
            {
                ToolTip = tooltip,
                ModulateSelfOverride = Color.Transparent
            };

            btn.AddChild(new TextureRect()
            {
                ToolTip = tooltip,
                TexturePath = proto.Icon,
                VerticalAlignment = Control.VAlignment.Center,
                HorizontalAlignment = Control.HAlignment.Center,
                TextureScale = proto.scale
            });

            btn.OnPressed += _ =>
            {
                urlMan.OpenUri(proto.Url);
            };

            reklama.Add(btn);
        }

        return reklama;
    }

}
