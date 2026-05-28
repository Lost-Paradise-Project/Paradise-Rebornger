using System.Linq;
using Content.Shared.Containers;
using Content.Shared.EntityTable;
using Robust.Shared.Containers;
using Content.Shared._LP.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._LP.Containers;

/// <summary>
/// Replenishes with a certain time interval the contents of things based on EntityTableContainerFill or by adding something else.
/// </summary>
public sealed partial class EntityTableRestockSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EntityTableRestockComponent>();

        while (query.MoveNext(out var uid, out var restock))
        {
            restock.Accumulator += TimeSpan.FromSeconds(frameTime);

            if (restock.Accumulator < restock.RestockTime)
                continue;

            restock.Accumulator = TimeSpan.Zero;

            TryRestock(uid, restock);
        }
    }

    private void TryRestock(EntityUid uid, EntityTableRestockComponent restock)
    {
        if (!TryComp<EntityTableContainerFillComponent>(uid, out var fill))
            return;

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        var xform = Transform(uid);
        var coords = new EntityCoordinates(uid, default);

        foreach (var (containerId, table) in fill.Containers)
        {
            if (!_container.TryGetContainer(uid, containerId, out var container, containerManager))
                continue;

            List<EntProtoId> spawns;

            if (restock.UseEntityTable)
            {
                spawns = _entityTable.GetSpawns(table).ToList();
            }
            else
            {
                spawns = restock.EntitiesToSpawn
                    .Select(x => new EntProtoId(x))
                    .ToList();
            }

            if (spawns.Count == 0)
                continue;

            if (!restock.ReplaceContents)
            {
                foreach (var proto in spawns)
                {
                    var spawned = Spawn(proto, coords);

                    if (_container.Insert(spawned, container, containerXform: xform))
                        continue;

                    QueueDel(spawned);
                }
                continue;
            }

            foreach (var ent in container.ContainedEntities.ToArray())
            {
                _container.Remove(ent, container);
                QueueDel(ent);
            }

            foreach (var proto in spawns)
            {
                var spawned = Spawn(proto, coords);

                if (!_container.Insert(spawned, container, containerXform: xform))
                {
                    _transform.AttachToGridOrMap(spawned);
                    continue;
                }
            }
        }
    }
}
