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
            ProcessGridFluid((uid, gridFluid, grid, xform));
            ProcessEvaporation((uid, gridFluid, grid, xform));
        }
    }

    /// <summary>
    /// Process a single grids fluid pools
    /// </summary>
    /// <param name="ent"></param>
    private void ProcessGridFluid(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var (owner, gridFluid, grid, xform) = ent;

        //TODO: ONLY DO ONE OF THESE STEPS PER TICK :-)

        switch (ent.Comp1.Stage)
        {
            case 1:
                ProcessInvalidTiles(ent);
                ent.Comp1.Stage++;
                break;
            case 2:
                ProcessActiveTiles(ent);
                ent.Comp1.Stage++;
                break;
            case 3:
                ProcessTileReactions(ent);
                CheckEmptyTiles(ent);
                ent.Comp1.Stage++;
                break;
            //case 4:
                //DrawTiles(ent);
                //ent.Comp1.Stage++;
                //break;
            case 4:
                UpdateFluidData(ent);
                ent.Comp1.Stage = 1;
                break;
            default:
                ent.Comp1.Stage = 4;
                break;
        }

        //ProcessInvalidTiles(ent);
        //ProcessActiveTiles(ent);
        //ProcessTileReactions(ent);
        //DrawTiles(ent);
    }

    private void CheckEmptyTiles(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var deleted = new List<Vector2i>();

        foreach (var (indices, tile) in ent.Comp1.Tiles)
        {
            if (tile.Solution.Volume == 0)
                deleted.Add(indices);
        }

        foreach (var tile in deleted)
        {
            var tilesol = ent.Comp1.Tiles[tile];

            RemoveActiveTile(ent.Comp1, tile);
            RemoveTile(ent.Comp1, tilesol);
        }

    }

    private void ProcessInvalidTiles(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var gridFluid = ent.Comp1;

        gridFluid.CurrentRunInvalidTiles.Clear();
        gridFluid.CurrentRunInvalidTiles.EnsureCapacity(gridFluid.InvalidTiles.Count);
        foreach (var indices in gridFluid.InvalidTiles)
        {
            gridFluid.CurrentRunInvalidTiles.Enqueue(indices);
        }
        //if (gridFluid.InvalidTiles.Count > 0)
        //    Log.Debug("invalid tile count: " + gridFluid.InvalidTiles.Count);

        while (gridFluid.CurrentRunInvalidTiles.TryDequeue(out var tile))
        {
            UpdateBlockedDirections(ent, tile, true);
        }
    }

    private void ProcessActiveTiles(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var gridFluid = ent.Comp1;

        gridFluid.CurrentRunTiles.Clear();
        gridFluid.CurrentRunTiles.EnsureCapacity(gridFluid.ActiveTiles.Count);
        foreach (var indices in gridFluid.ActiveTiles)
        {
            gridFluid.CurrentRunTiles.Enqueue(indices);
        }
        //if (gridFluid.ActiveTiles.Count > 0)
        //    Log.Debug("Active tile count: " + gridFluid.ActiveTiles.Count);

        while (gridFluid.CurrentRunTiles.TryDequeue(out var indices))
        {
            if (!gridFluid.Tiles.TryGetValue(indices, out var tile))
                continue;

            ProcessFluidSpread(ent, tile);
        }
    }

    private void ProcessTileReactions(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var gridFluid = ent.Comp1;

        gridFluid.CurrentRunUnreactedTiles.Clear();
        gridFluid.CurrentRunUnreactedTiles.EnsureCapacity(gridFluid.UnreactedTiles.Count);
        foreach (var group in gridFluid.UnreactedTiles)
        {
            gridFluid.CurrentRunUnreactedTiles.Enqueue(group);
        }
        //if (gridFluid.UnreactedTiles.Count > 0)
        //    Log.Debug("tile reaction count: " + gridFluid.UnreactedTiles.Count);

        while (gridFluid.CurrentRunUnreactedTiles.TryDequeue(out var tile))
        {
            FullyReactSolution(gridFluid, tile);
            gridFluid.UnreactedTiles.Remove(tile);
        }
    }
}
