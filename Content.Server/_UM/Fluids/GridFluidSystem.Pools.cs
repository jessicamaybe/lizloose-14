using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem
{
    private void ProcessPoolSpread(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent, FluidPool pool)
    {
        var gridFluid = ent.Comp1;
        if (!gridFluid.FluidPools.Contains(pool))
            return;

        Log.Debug("processing pool! Volume: " + pool.Solution.Volume + " Size: " + pool.Indices.Count);

        var indices = pool.Indices.ToList();



        //In case we are on top of any tiles
        foreach (var tile in pool.Indices)
        {
            if (!TryGetFluid(ent.Comp1, tile, out var tileSolution))
                continue;

            AddTileToPool(ent.Comp1, tileSolution, pool, false);
        }


        if (pool.Volume / pool.Indices.Count < gridFluid.OverflowVolume)
        {
            Log.Debug("breaking down pool");
            BreakdownPool((ent.Owner, gridFluid), pool);
            return;
        }

        var newTiles = new HashSet<Vector2i>();
        var mergers = new HashSet<FluidPool>();

        var edges = pool.Edges;

        foreach (var (edge, neighbors) in edges)
        {
            foreach (var tile in neighbors)
            {
                if (IsTileBlocked((ent.Owner, ent.Comp2, ent.Comp3), tile))
                    continue;
                if (TryGetFluid(gridFluid, tile, out var tileSolution))
                {
                    AddTileToPool(gridFluid, tileSolution, pool, false);
                    continue;
                }

                if (TryGetPool(gridFluid, tile, out var neighborPool))
                {
                    mergers.Add(neighborPool);
                    continue;
                }

                newTiles.Add(tile);
            }
        }

        pool.Indices.UnionWith(newTiles);

        foreach (var neighborPool in mergers)
        {
            Log.Debug("merging pool");
            if (neighborPool == pool)
                continue;
            if (!gridFluid.FluidPools.Contains(neighborPool))
                continue;
            MergePools(ent.Comp1, neighborPool, pool);
        }

        RecomputePool(pool);
    }

    private void CreatePool(Entity<GridFluidComponent> ent, List<Vector2i> indices)
    {
        var pool = new FluidPool();

        foreach (var tile in indices)
        {
            if (!TryGetFluid(ent.Comp, tile, out var tileSolution))
                continue;

            pool.Solution.AddSolution(tileSolution.Solution, _prototypeManager);
            pool.Indices.Add(tile);
            RemoveTile(ent.Comp, tileSolution);
        }

        Log.Debug("Pool created. Volume: " + pool.Solution.Volume + " Size: " + pool.Indices.Count);
        ent.Comp.FluidPools.Add(pool);
        RecomputePool(pool);
    }

    private bool TryGetPool(GridFluidComponent gridFluid, Vector2i indices, [NotNullWhen(true)] out FluidPool? fluidPool)
    {
        fluidPool = null;

        foreach (var pool in gridFluid.FluidPools)
        {
            if (!pool.Indices.Contains(indices))
                continue;
            fluidPool = pool;
            return true;
        }
        return false;
    }

    private void MergePools(GridFluidComponent gridFluid, FluidPool origin, FluidPool target, bool recompute = true)
    {
        target.Solution.AddSolution(origin.Solution, _prototypeManager);
        foreach (var tile in origin.Indices)
        {
            target.Indices.Add(tile);

        }
        gridFluid.FluidPools.Remove(origin);
        if (recompute)
            RecomputePool(target);
    }

    private void AddTileToPool(GridFluidComponent gridFluid, TileSolution tileSolution, FluidPool pool, bool recompute = true)
    {
        pool.Solution.AddSolution(tileSolution.Solution, _prototypeManager);
        pool.Indices.Add(tileSolution.GridIndices);
        RemoveTile(gridFluid, tileSolution);
        if (recompute)
            RecomputePool(pool);
    }

    private void RecomputePool(FluidPool pool)
    {
        pool.Volume = pool.Solution.Volume;
        pool.Color = pool.Solution.GetColor(_prototypeManager);

        pool.Edges.Clear();
        foreach (var tile in pool.Indices)
        {
            var neighborCount = 0;
            var neighbors = new List<Vector2i>();
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                var neighborPos = tile.Offset(direction);
                if (pool.Indices.Contains(neighborPos))
                {
                    neighborCount++;
                    continue;
                }
                neighbors.Add(neighborPos);
            }

            if (neighborCount == 0 || neighborCount < 4)
                pool.Edges.Add(tile, neighbors);
        }
    }

    private void BreakdownPool(Entity<GridFluidComponent> ent, FluidPool pool)
    {
        var splitAmount = pool.Solution.Volume / pool.Indices.Count;
        Log.Debug("Breaking down pool!");
        Log.Debug("Split amount: " + splitAmount);

        foreach (var tile in pool.Indices)
        {
            AddTile(ent, tile, pool.Solution.SplitSolution(splitAmount));
            AddTileReaction(ent.Comp, tile);
        }
        ent.Comp.FluidPools.Remove(pool);
    }
}
