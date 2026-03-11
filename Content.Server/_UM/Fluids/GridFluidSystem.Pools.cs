using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Stunnable;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem
{
    [Dependency] private readonly StunSystem _stuns = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    private EntityQuery<ProjectileComponent> _projQuery;

    private void ProcessPoolSpread(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent, FluidPool pool)
    {
        var gridFluid = ent.Comp1;
        if (!gridFluid.FluidPools.Contains(pool))
            return;

        //In case we are on top of any tiles
        foreach (var tile in pool.Indices)
        {
            if (!TryGetFluid(ent.Comp1, tile, out var tileSolution))
                continue;

            AddTileToPool(ent.Comp1, tileSolution, pool, false);
        }


        if (pool.Volume / pool.Indices.Count < gridFluid.OverflowVolume)
        {
            BreakdownPool((ent.Owner, gridFluid), pool);
            return;
        }

        var newTiles = new Dictionary<Vector2i, Vector2i>();
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

                newTiles.TryAdd(edge, tile);
            }
        }

        foreach (var (edge, tile) in newTiles)
        {
            pool.Indices.Add(tile);
            KnockdownPeople((ent.Owner, ent.Comp1, ent.Comp2), edge, tile);
        }

        foreach (var neighborPool in mergers)
        {
            if (neighborPool == pool)
                continue;
            if (!gridFluid.FluidPools.Contains(neighborPool))
                continue;
            MergePools(ent.Comp1, neighborPool, pool);
        }

        RecomputePool(pool);
    }

    private void KnockdownPeople(Entity<GridFluidComponent, MapGridComponent> ent, Vector2i origin, Vector2i destination)
    {
        var originCoords = _map.GridTileToWorld(ent.Owner, ent.Comp2, origin);
        var destinationCoords = _map.GridTileToWorld(ent.Owner, ent.Comp2, destination);

        var entitiesInRange = _lookup.GetEntitiesInRange(destinationCoords, 1f, LookupFlags.All);

        var direction = (destinationCoords.Position - originCoords.Position);

        foreach (var victim in entitiesInRange)
        {
            if (!TryComp<PhysicsComponent>(victim, out var physics))
                continue;

            _stuns.TryCrawling(victim, TimeSpan.FromSeconds(3));
            _throwing.TryThrow(victim, direction, physics, Transform(victim), _projQuery, direction.Length() / 6, null, 2f, null, false, true, true, false, false, ThrowingUnanchorStrength.Unanchorable);
            //_physics.ApplyLinearImpulse(victim, direction * (physics.Mass * 4)); //lmao
        }
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

        foreach (var tile in pool.Indices)
        {
            AddTile(ent, tile, pool.Solution.SplitSolution(splitAmount));
            AddTileReaction(ent.Comp, tile);
        }
        ent.Comp.FluidPools.Remove(pool);
    }
}
