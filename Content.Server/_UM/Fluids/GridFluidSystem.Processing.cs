using System.Diagnostics;
using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
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

        foreach (var (indices, tile) in gridFluid.Tiles)
        {
            Log.Debug("Tile volume: " + tile.Solution.Volume);

            if (!tile.Excited)
                continue;

            gridFluid.ExcitedTiles.Add(indices);
            tile.Excited = false;
        }

        foreach (var indices in gridFluid.ExcitedTiles)
        {
            if (!gridFluid.Tiles.TryGetValue(indices, out var tileSolution))
                continue;
            ProcessFluidSpread(ent, indices, tileSolution);

            var fillLevel = CalculateFillLevel(ent, tileSolution);

            if (tileSolution.FillLevel != fillLevel)
            {
                if (gridFluid.DrawnTiles.TryGetValue(indices, out var tileent) &&
                    fillLevel != tileSolution.FillLevel)
                {
                    QueueDel(tileent);
                    gridFluid.DrawnTiles.Remove(indices);
                }
            }

            tileSolution.FillLevel = fillLevel;
        }
        gridFluid.ExcitedTiles.Clear();
        DrawTiles(ent);
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

            Log.Debug("Drawing tile at: " + indices);

            var coords = _map.GridTileToLocal(owner, grid, indices);

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
            gridFluid.DrawnTiles.Add(indices, spawned);
        }
        foreach (var (indices, tileEnt) in gridFluid.DrawnTiles)
        {
            if (!gridFluid.Tiles.ContainsKey(indices))
                QueueDel(tileEnt);
        }
    }
}
