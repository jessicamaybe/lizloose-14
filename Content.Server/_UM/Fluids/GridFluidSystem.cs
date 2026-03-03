using System.Diagnostics.CodeAnalysis;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem : SharedGridFluidSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializePools();
        InitializeSource();

        SubscribeLocalEvent<GridFluidComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GridFluidComponent>();

        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {

            if (comp.NextUpdate > curTime)
                continue;

            UpdatePuddles((uid, comp));

            comp.NextUpdate += comp.UpdateInterval;
        }
    }

    private void OnMapInit(Entity<GridFluidComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    private void UpdatePuddles(Entity<GridFluidComponent> ent)
    {
        foreach (var pool in ent.Comp.DeletedTiles)
        {
            ent.Comp.Pools.Remove(pool);
            ent.Comp.StalePools.Remove(pool);
            QueueDel(pool);
        }

        QueuePools(ent.Comp.CurrentRunPools, ent.Comp.StalePools);

        while (ent.Comp.CurrentRunPools.TryDequeue(out var pool))
        {
            ent.Comp.StalePools.Remove(pool);
            pool.Comp.EdgeTiles = GetEdgeTiles(pool);
            RemoveDeleted(pool);
            AddQueuedTiles(pool);
            UpdatePool(pool);
            ShittyDraw(pool);
            CheckIntersect(ent, pool);
        }
    }

    private void QueuePools(
        Queue<Entity<FluidPoolComponent>> queue,
        HashSet<Entity<FluidPoolComponent>> pools)
    {

        queue.Clear();
        queue.EnsureCapacity(pools.Count);
        foreach (var tile in pools)
        {
            queue.Enqueue(tile);
        }
    }

    private void CheckIntersect(Entity<GridFluidComponent> ent, Entity<FluidPoolComponent> poolEnt)
    {
        var removedPool = false;

        foreach (var pool in ent.Comp.Pools)
        {
            if (pool.Owner == poolEnt.Owner)
                continue;

            if (pool.Comp.Tiles.Overlaps(poolEnt.Comp.Tiles))
            {
                if (MergePools(ent, poolEnt, pool))
                    removedPool = true;
                break;
            }
        }

        if (removedPool)
            ent.Comp.DeletedTiles.Add(poolEnt);
    }

    private bool MergePools(Entity<GridFluidComponent> ent, Entity<FluidPoolComponent> pool, Entity<FluidPoolComponent> target)
    {
        if (!_solutionContainer.ResolveSolution(pool.Owner,
                pool.Comp.SolutionName,
                ref pool.Comp.Solution,
                out var poolSolution))
            return false;

        if (!_solutionContainer.ResolveSolution(target.Owner,
                target.Comp.SolutionName,
                ref target.Comp.Solution,
                out var targetSolution))
            return false;

        if (targetSolution.Volume == 0)
        {
            ent.Comp.DeletedTiles.Add(target);
            return false;
        }

        _solutionContainer.TryTransferSolution(target.Comp.Solution.Value, poolSolution, poolSolution.Volume);

        target.Comp.Tiles.UnionWith(pool.Comp.Tiles);
        foreach (var tile in pool.Comp.DrawnTiles)
        {
            QueueDel(tile.Value);
        }

        DirtyPool(target);
        return true;
    }

    private bool ResolveGridFluid(Entity<FluidPoolComponent> ent, [NotNullWhen(true)] out Entity<GridFluidComponent>? entity)
    {
        entity = null;

        if (!TryComp<GridFluidComponent>(ent.Comp.GridUid, out var gridFluid))
            return false;

        entity =  (ent.Comp.GridUid, gridFluid);
        return true;
    }

    private void DirtyPool(Entity<FluidPoolComponent> ent)
    {
        if (ResolveGridFluid(ent, out var gridFluid))
            gridFluid.Value.Comp.StalePools.Add(ent);
    }

    private bool TryGetPool(Entity<MapGridComponent> ent, EntityCoordinates coords, [NotNullWhen(true)] out Entity<FluidPoolComponent>? fluidPool)
    {
        fluidPool = null;

        if (!TryComp<GridFluidComponent>(ent, out var gridFluidComponent))
            return false;

        if (!_map.TryGetTileRef(ent.Owner, ent.Comp, coords, out var tileRef))
            return false;

        foreach (var pool in gridFluidComponent.Pools)
        {
            if (pool.Comp.EdgeTiles.Contains(tileRef))
            {
                fluidPool = pool;
                return true;
            }
        }
        return false;
    }
}
