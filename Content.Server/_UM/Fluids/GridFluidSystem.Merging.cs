using Content.Shared._UM.Fluids.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This puddle merging behavior
/// </summary>
public sealed partial class GridFluidSystem
{

    private void StartMerge(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent,
        Entity<FluidPoolComponent> poolA,
        Entity<FluidPoolComponent> poolB,
        TileRef tile)
    {
        if (!_solutionContainer.ResolveSolution(poolA.Owner,
                poolA.Comp.SolutionName,
                ref poolA.Comp.Solution,
                out var poolASolution))
            return;

        if (!_solutionContainer.ResolveSolution(poolB.Owner,
                poolB.Comp.SolutionName,
                ref poolB.Comp.Solution,
                out var poolBSolution))
            return;

        var poolAVolume = poolASolution.Volume;
        var poolBVolume = poolBSolution.Volume;
        var averageOverflow = (poolA.Comp.OverflowVolume + poolB.Comp.OverflowVolume) / 2;

        var difference = FixedPoint2.FromCents(Math.Abs(poolAVolume.Value - poolBVolume.Value));

        var steps = (difference.Value / averageOverflow.Value);
        steps = Math.Clamp(steps, 3, 20);
        var merger = new PuddleMerger(tile.GridIndices, difference, steps, poolA, poolB);
        ent.Comp1.Mergers.Add(tile, merger);
    }

    private void ProcessMergers(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var (owner, gridFluid, grid, xform) = ent;

        var finishedMergers = new List<TileRef>();

        foreach (var (tileRef, merger) in ent.Comp1.Mergers)
        {
            if (merger.Steps == 0)
            {
                QuickMerge((owner, gridFluid), tileRef, merger.PoolA, merger.PoolB);
                finishedMergers.Add(tileRef);
                continue;
            }

            if (ent.Comp1.DeletedPools.Contains(merger.PoolA) || ent.Comp1.DeletedPools.Contains(merger.PoolB)
                || !ent.Comp1.Pools.Contains(merger.PoolA) || !ent.Comp1.Pools.Contains(merger.PoolB))
            {
                finishedMergers.Add(tileRef);
                continue;
            }

            if (!_solutionContainer.ResolveSolution(merger.PoolA.Owner,
                    merger.PoolA.Comp.SolutionName,
                    ref merger.PoolA.Comp.Solution,
                    out var poolASolution))
                continue;

            if (!_solutionContainer.ResolveSolution(merger.PoolB.Owner,
                    merger.PoolB.Comp.SolutionName,
                    ref merger.PoolB.Comp.Solution,
                    out var poolBSolution))
                continue;

            var mixEnt = SpawnAtPosition(null, _map.GridTileToLocal(ent.Owner, ent.Comp2, merger.Indices));

            if (!_solutionContainer.EnsureSolutionEntity(mixEnt, "pool", out var tempSolution,100000))
                continue;

            var transferAmount = merger.Steps * merger.PoolA.Comp.OverflowVolume;

            _solutionContainer.TryTransferSolution(tempSolution.Value, poolASolution, transferAmount);
            _solutionContainer.TryTransferSolution(tempSolution.Value, poolBSolution, transferAmount);

            _solutionReaction.FullyReactSolution(tempSolution.Value);

            _solutionContainer.TryTransferSolution(merger.PoolA.Comp.Solution.Value, tempSolution.Value.Comp.Solution, transferAmount);
            _solutionContainer.TryTransferSolution(merger.PoolB.Comp.Solution.Value, tempSolution.Value.Comp.Solution, transferAmount);
            QueueDel(mixEnt);
            merger.Steps--;
        }

        foreach (var merger in finishedMergers)
        {
            ent.Comp1.Mergers.Remove(merger);
        }
    }

    /// <summary>
    /// In one step, merge the contents of pool A into pool B
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="mergeTile"></param>
    /// <param name="poolA"></param>
    /// <param name="poolB"></param>
    /// <returns></returns>
    private void QuickMerge(Entity<GridFluidComponent> ent, TileRef mergeTile, Entity<FluidPoolComponent> poolA, Entity<FluidPoolComponent> poolB)
    {
        if (!_solutionContainer.ResolveSolution(poolA.Owner,
                poolA.Comp.SolutionName,
                ref poolA.Comp.Solution,
                out var poolSolution))
            return;

        if (!_solutionContainer.ResolveSolution(poolB.Owner,
                poolB.Comp.SolutionName,
                ref poolB.Comp.Solution,
                out var targetSolution))
            return;

        if (targetSolution.Volume == 0)
        {
            ent.Comp.DeletedPools.Add(poolB);
            return;
        }

        if (!GetTileCoords(poolB, mergeTile, out var coords))
            return;

        _solutionContainer.TryTransferSolution(poolB.Comp.Solution.Value, poolSolution, poolSolution.Volume);
        _solutionReaction.FullyReactSolution(poolB.Comp.Solution.Value);

        if (targetSolution.Volume == 0)
            ent.Comp.DeletedPools.Add(poolB);

        poolB.Comp.Tiles.UnionWith(poolA.Comp.Tiles);
        foreach (var tile in poolA.Comp.DrawnTiles)
        {
            QueueDel(tile.Value);
        }
        ent.Comp.DeletedPools.Add(poolA);

        DirtyPool(poolB);
        return;
    }


}
