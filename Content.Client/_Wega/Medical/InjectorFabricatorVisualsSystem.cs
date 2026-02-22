using Robust.Client.GameObjects;
using Content.Shared._Wega.Medical;

namespace Content.Client._Wega.Medical;

public sealed class InjectorFabricatorSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorFabricatorComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, InjectorFabricatorComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, InjectorFabricatorVisuals.IsRunning, out var isRunning, args.Component))
            return;

        _sprite.LayerSetVisible(uid, InjectorFabricatorVisuals.IsRunning, isRunning);
    }
}
