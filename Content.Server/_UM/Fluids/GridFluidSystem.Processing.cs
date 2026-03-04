using Content.Shared._UM.Fluids.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{

    private void UpdateFluidProcessing(float frameTime)
    {
        var query = EntityQueryEnumerator<GridFluidComponent, MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var gridFluid, out var grid, out var xform))
        {
            ProcessGridPool((uid, gridFluid, grid, xform));
        }
    }

    /// <summary>
    /// Process a single grids fluid pools
    /// </summary>
    /// <param name="ent"></param>
    private void ProcessGridPool(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var (owner, gridFluid, grid, xform) = ent;

        //process pool delete queue
        foreach (var pool in gridFluid.DeletedPools)
        {
            gridFluid.Pools.Remove(pool);
            gridFluid.StalePools.Remove(pool);
            ShittyDrawDelete(pool);
            QueueDel(pool);
        }
        gridFluid.DeletedPools.Clear();

        //Add or remove tiles
        foreach (var pool in gridFluid.Pools)
        {
            RemoveDeleted(pool);
            AddQueuedTiles(pool);
        }
        var curTime = _timing.CurTime;

        if (gridFluid.NextUpdate > curTime)
            return;

        ProcessMergers(ent);

        QueuePools(gridFluid.CurrentRunPools, gridFluid.StalePools);

        while (gridFluid.CurrentRunPools.TryDequeue(out var pool))
        {
            gridFluid.StalePools.Remove(pool);
            pool.Comp.EdgeTiles = GetEdgeTiles(pool);

            CheckIntersect(ent, pool);
            ProcessFluids(ent, pool);
            ShittyDraw(pool);
        }

        gridFluid.NextUpdate += gridFluid.UpdateInterval;
    }

    /// <summary>
    /// Processes a single pool of fluid
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="pool"></param>
    private void ProcessFluids(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent, Entity<FluidPoolComponent> pool)
    {
        var (owner, gridFluid, grid, xform) = ent;

        var curTime = _timing.CurTime;

        if (IsOverflowing(pool))
        {
            SpreadPool(ent, pool);
            ShittyDraw(pool);
        }

        if (gridFluid.NextUpdate > curTime)
            return;

        gridFluid.NextUpdate += gridFluid.UpdateInterval;
    }
}
