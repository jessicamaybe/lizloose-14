using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{
    public void AddFluid(Entity<MapGridComponent> ent, EntityCoordinates coords, Solution solution)
    {
        if (!_map.TryGetTileRef(ent.Owner, ent.Comp, coords, out var tileRef))
            return;

        var gridFluidComp = EnsureComp<GridFluidComponent>(ent.Owner);

        Log.Debug("Trying to spawn pool");
        var pool = Spawn("FluidPool", coords);
        var poolComp = EnsureComp<FluidPoolComponent>(pool);
        poolComp.GridUid = ent.Owner;

        poolComp.Tiles.Add(tileRef);
        gridFluidComp.Pools.Add((pool, poolComp));

        if (_solutionContainer.ResolveSolution(pool,
                poolComp.SolutionName,
                ref poolComp.Solution,
                out _))
        {
            Log.Debug("Added solution to pool");
            _solutionContainer.TryAddSolution(poolComp.Solution.Value, solution);
            DirtyPool((pool, poolComp));
        }
    }

    public void TryRemoveFluid(Entity<MapGridComponent> ent, EntityCoordinates coords, FixedPoint2 amount)
    {
        if (!TryGetPool(ent, coords, out var pool))
            return;

        if (!_map.TryGetTileRef(ent.Owner, ent.Comp, coords, out var tileRef))
            return;

        if (!_solutionContainer.ResolveSolution(pool.Value.Owner, pool.Value.Comp.SolutionName, ref pool.Value.Comp.Solution, out _))
            return;

        _solutionContainer.SplitSolution(pool.Value.Comp.Solution.Value, amount);

        var volume = CurrentVolume(pool.Value);
        var amountPerTile = volume / (pool.Value.Comp.Tiles.Count - 1);
        if (amountPerTile > pool.Value.Comp.OverflowVolume)
            return;

        RemoveTile(pool.Value, tileRef);
    }

}
