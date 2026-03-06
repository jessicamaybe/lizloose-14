using System.Diagnostics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
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
        }
    }

    /// <summary>
    /// Process a single grids fluid pools
    /// </summary>
    /// <param name="ent"></param>
    private void ProcessGridFluid(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var (owner, gridFluid, grid, xform) = ent;

        ProcessInvalidTiles(ent);
        ProcessActiveTiles(ent);
        ProcessTileReactions(ent);
        //ProcessTileGroups(ent);
        DrawTiles(ent);
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

            var fillLevel = CalculateFillLevel(ent, tile);
            if (tile.FillLevel != fillLevel)
            {
                if (gridFluid.DrawnTiles.TryGetValue(indices, out var tileent) &&
                    fillLevel != tile.FillLevel)
                {
                    QueueDel(tileent);
                    gridFluid.DrawnTiles.Remove(indices);
                }
            }
            tile.FillLevel = fillLevel;
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

    private void ProcessTileGroups(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var gridFluid = ent.Comp1;

        gridFluid.CurrentRunTileGroups.Clear();
        gridFluid.CurrentRunTileGroups.EnsureCapacity(gridFluid.TileGroups.Count);
        foreach (var group in gridFluid.TileGroups)
        {
            gridFluid.CurrentRunTileGroups.Enqueue(group);
        }
        //if (gridFluid.TileGroups.Count > 0)
        //    Log.Debug("tile group count: " + gridFluid.TileGroups.Count);

        while (gridFluid.CurrentRunTileGroups.TryDequeue(out var tileGroup))
        {
            tileGroup.BreakdownCooldown++;
            tileGroup.DismantleCooldown++;

            var splitAmount = FixedPoint2.Zero;

            if (tileGroup.Tiles.Count > 0)
            {
                foreach (var tile in tileGroup.Tiles)
                {
                    splitAmount += tile.LastShareVolume;
                    tile.LastShareVolume = tile.ShareVolume;
                }

                tileGroup.LastAverage = tileGroup.Average;
                tileGroup.Average = splitAmount.Value / tileGroup.Tiles.Count;

                var diff = Math.Abs(tileGroup.LastAverage - tileGroup.Average);

                Log.Debug("average: " + diff);

                if (diff > 50)
                {
                    ExcitedGroupResetCooldowns(tileGroup);
                    continue;
                }
                if (diff > 14)
                    tileGroup.DismantleCooldown = 0;
            }

            Log.Debug("breakdown cooldown: " + tileGroup.BreakdownCooldown);
            Log.Debug("Dismantle cooldown: " + tileGroup.DismantleCooldown);

            if (tileGroup.BreakdownCooldown > 4)
                TileGroupSelfBreakdown(ent, tileGroup);
            if (tileGroup.DismantleCooldown > 12)
                DeactivateGroupTiles(gridFluid, tileGroup);
        }
    }

    private FillLevel CalculateFillLevel(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent,
        TileSolution tile)
    {
        var (owner, gridFluid, grid, xform) = ent;

        if (tile.Solution.Volume > gridFluid.OverflowVolume * 4)
            return FillLevel.Ceiling;

        if (tile.Solution.Volume > gridFluid.OverflowVolume * 3)
            return FillLevel.Waist;

        if (tile.Solution.Volume > gridFluid.OverflowVolume * 2)
            return FillLevel.Ankle;

        return FillLevel.Puddle;
    }

    private void DrawTiles(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var (owner, gridFluid, grid, xform) = ent;

        foreach (var (indices, tile) in gridFluid.Tiles)
        {
            if (gridFluid.DrawnTiles.ContainsKey(indices))
                continue;

            //Log.Debug("Drawing tile at: " + indices);

            var coords = _map.GridTileToLocal(owner, grid, indices);
            tile.FillLevel = CalculateFillLevel(ent, tile);

            var proto = "FluidTest25";
            switch (tile.FillLevel)
            {
                default:
                    proto = "FluidTest25";
                    break;
                case FillLevel.Ankle:
                    proto = "FluidTest50";
                    break;
                case FillLevel.Waist:
                    proto = "FluidTest75";
                    break;
                case FillLevel.Ceiling:
                    proto = "FluidTest100";
                    break;
            }

            var spawned = Spawn(proto, coords);
            var relay = EnsureComp<TileSolutionRelayComponent>(spawned);
            relay.TileSolution = tile;
            var pipeColor = EnsureComp<AtmosPipeColorComponent>(spawned);
            var color = tile.Solution.GetColor(_prototypeManager);
            _pipeColor.SetColor(spawned, pipeColor, color);
            gridFluid.DrawnTiles.Add(indices, spawned);
        }
        foreach (var (indices, tileEnt) in gridFluid.DrawnTiles)
        {
            if (!gridFluid.Tiles.ContainsKey(indices))
                QueueDel(tileEnt);
        }
    }
}
