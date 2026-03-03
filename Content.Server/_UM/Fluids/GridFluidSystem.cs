using System.Linq;
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
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
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

    private void AddFluid(Entity<MapGridComponent> ent, EntityCoordinates coords, Solution solution)
    {
        if (!_map.TryGetTileRef(ent.Owner, ent.Comp, coords, out var tileRef))
            return;

        var gridFluidComp = EnsureComp<GridFluidComponent>(ent.Owner);

        Log.Debug("Trying to spawn pool");
        var pool = Spawn("FluidPool", coords);
        var poolComp = EnsureComp<FluidPoolComponent>(pool);
        poolComp.GridUid = ent.Owner;

        poolComp.Tiles.Add(tileRef);
        gridFluidComp.Pools.Add(pool);

        if (_solutionContainerSystem.ResolveSolution(pool,
                poolComp.SolutionName,
                ref poolComp.Solution,
                out _))
        {
            Log.Debug("Added solution to pool");
            _solutionContainerSystem.TryAddSolution(poolComp.Solution.Value, solution);
            poolComp.NeedsUpdate = true;
        }
    }

    private void UpdatePuddles(Entity<GridFluidComponent> ent)
    {
        var pools = ent.Comp.Pools;

        foreach (var pool in pools)
        {
            if (Deleted(pool))
                continue;

            if (!TryComp<FluidPoolComponent>(pool, out var poolComp))
                continue;

            if (poolComp.NeedsUpdate)
            {
                UpdatePool((pool, poolComp));
                ShittyDraw((pool, poolComp));
                CheckIntersect(ent, (pool, poolComp));
            }
        }

        foreach (var pool in ent.Comp.DeleteQueue)
        {
            QueueDel(pool);
        }
    }

    private void CheckIntersect(Entity<GridFluidComponent> ent, Entity<FluidPoolComponent> poolEnt)
    {
        var removedPool = false;

        foreach (var pool in ent.Comp.Pools)
        {
            if (pool == poolEnt.Owner)
                continue;

            if (!TryComp<FluidPoolComponent>(pool, out var poolComponent))
                continue;

            if (poolComponent.Tiles.Overlaps(poolEnt.Comp.Tiles))
            {
                MergePools(ent, poolEnt, pool);
                removedPool = true;
                break;
            }
        }

        if (removedPool)
            ent.Comp.DeleteQueue.Add(poolEnt);
    }

    private void MergePools(Entity<GridFluidComponent> ent, Entity<FluidPoolComponent> pool, Entity<FluidPoolComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        if (!_solutionContainerSystem.ResolveSolution(pool.Owner,
                pool.Comp.SolutionName,
                ref pool.Comp.Solution,
                out var poolSolution))
            return;

        if (!_solutionContainerSystem.ResolveSolution(target.Owner,
                target.Comp.SolutionName,
                ref target.Comp.Solution,
                out var targetSolution))
            return;

        _solutionContainerSystem.AddSolution(target.Comp.Solution.Value, poolSolution);

        target.Comp.Tiles.UnionWith(pool.Comp.Tiles);
        foreach (var tile in pool.Comp.DrawnTiles)
        {
            QueueDel(tile.Value);
        }
        target.Comp.NeedsUpdate = true;
    }


    private void AddPuddle(Entity<GridFluidComponent> ent)
    {


    }

}
