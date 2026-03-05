using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{

    private void TileGroupSelfBreakdown(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent, TileSolutionGroup tileGroup)
    {
        var gridFluid = ent.Comp1;

        var combinedSolution = new Solution(100000000);

        var tiles = tileGroup.Tiles.Count;
        if (tileGroup.Disposed)
            return;

        Log.Debug("Breakdown of excited group with tiles: " + tiles);

        if (tiles == 0)
        {
            ExcitedGroupDispose(ent.Comp1, tileGroup);
            return;
        }

        foreach (var tile in tileGroup.Tiles)
        {
            if (tile.Solution.Volume == 0)
                continue;

            combinedSolution.AddSolution(tile.Solution, _prototypeManager);
        }

        var splitVolume = combinedSolution.Volume / tileGroup.Tiles.Count;
        Log.Debug("Merging tiles at: " + splitVolume + "  for each tile");

        foreach (var tile in tileGroup.Tiles)
        {
            tile.Solution = combinedSolution.SplitSolution(splitVolume);
        }

        tileGroup.BreakdownCooldown = 0;
    }

    private void ExcitedGroupMerge(GridFluidComponent gridFluid, TileSolutionGroup ourGroup, TileSolutionGroup otherGroup)
    {
        var ourSize = ourGroup.Tiles.Count;
        var otherSize = otherGroup.Tiles.Count;

        TileSolutionGroup winner;
        TileSolutionGroup loser;

        if (ourSize > otherSize)
        {
            winner = ourGroup;
            loser = otherGroup;
        }
        else
        {
            winner = otherGroup;
            loser = ourGroup;
        }

        foreach (var tile in loser.Tiles)
        {
            tile.TileSolutionGroup = winner;
            winner.Tiles.Add(tile);
        }

        loser.Tiles.Clear();
        ExcitedGroupDispose(gridFluid, loser);
        ExcitedGroupResetCooldowns(winner);
    }

    private void ExcitedGroupDispose(GridFluidComponent gridFluid, TileSolutionGroup excitedGroup)
    {
        if (excitedGroup.Disposed)
            return;

        excitedGroup.Disposed = true;
        gridFluid.TileGroups.Remove(excitedGroup);

        foreach (var tile in excitedGroup.Tiles)
        {
            tile.TileSolutionGroup = null;
        }
        excitedGroup.Tiles.Clear();
    }

    private void DeactivateGroupTiles(GridFluidComponent gridFluid, TileSolutionGroup tileGroup)
    {
        foreach (var tile in tileGroup.Tiles)
        {
            tile.TileSolutionGroup = null;
            RemoveActiveTile(gridFluid, tile.GridIndices);
        }

        tileGroup.Tiles.Clear();
    }


    private void ExcitedGroupResetCooldowns(TileSolutionGroup tileGroup)
    {
        tileGroup.BreakdownCooldown = 0;
        tileGroup.DismantleCooldown = 0;
    }

    private void TileGroupAddTile(TileSolutionGroup tileGroup, TileSolution tile)
    {
        tileGroup.Tiles.Add(tile);
        tile.TileSolutionGroup = tileGroup;
        ExcitedGroupResetCooldowns(tileGroup);
    }
    private void TileGroupRemoveTile(TileSolutionGroup tileGroup, TileSolution tile)
    {
        tileGroup.Tiles.Remove(tile);
        tile.TileSolutionGroup = null;
    }

}
