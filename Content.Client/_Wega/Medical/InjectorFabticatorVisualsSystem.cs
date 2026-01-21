using Robust.Client.GameObjects;
using Content.Shared._Wega.Medical;

namespace Content.Client._Wega.Medical;

public sealed class InjectorFabticatorSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorFabticatorComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, InjectorFabticatorComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, InjectorFabticatorVisuals.IsRunning, out var isRunning, args.Component))
            return;

        _sprite.LayerSetVisible(uid, InjectorFabticatorVisuals.IsRunning, isRunning);
    }
}
