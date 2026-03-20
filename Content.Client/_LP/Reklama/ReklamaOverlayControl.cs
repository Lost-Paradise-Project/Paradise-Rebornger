using System.Numerics;
using Content.Shared._LP.Reklama;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._LP.Reklama;

public sealed class ReklamaControlManager
{
    public static List<Control> GetAdIcons()
    {
        var protos = IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes<ReklamaPrototype>();
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
                TextureScale = new Vector2(2, 2),
                TexturePath = proto.Icon,
                VerticalAlignment = Control.VAlignment.Center,
                HorizontalAlignment = Control.HAlignment.Center
            });

            btn.OnPressed += _ =>
            {
                IoCManager.Resolve<IUriOpener>().OpenUri(proto.Url);
            };

            reklama.Add(btn);
        }

        return reklama;
    }

}
