using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem
{
    private void InitializePools()
    {
        SubscribeLocalEvent<FluidPoolComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(Entity<FluidPoolComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.SolutionName)
            return;

        DirtyPool(ent);
    }

    /// <summary>
    /// Remove the deleted tiles in the queue
    /// </summary>
    /// <param name="ent"></param>
    private void RemoveDeleted(Entity<FluidPoolComponent> ent)
    {
        if (ent.Comp.RemovedTiles.Count == 0)
            return;

        foreach (var tile in ent.Comp.RemovedTiles)
        {
            ent.Comp.Tiles.Remove(tile);
            ent.Comp.EdgeTiles = GetEdgeTiles(ent);

            if (ent.Comp.DrawnTiles.TryGetValue(tile, out var tileEnt))
            {
                QueueDel(tileEnt);
                ent.Comp.DrawnTiles.Remove(tile);
            }
        }
        ent.Comp.RemovedTiles.Clear();
        DirtyPool(ent);
    }

    private void ShittyDraw(Entity<FluidPoolComponent> ent)
    {
        foreach (var tile in ent.Comp.Tiles)
        {
            if (ent.Comp.DrawnTiles.ContainsKey(tile))
                continue;

            if (GetTileCoords(ent, tile, out var coords))
            {
                var proto = "FluidTest25";

                if (ent.Comp.FillLevel > PoolFillLevel.Puddle)
                    proto = "FluidTest50";

                var spawned = Spawn(proto, coords.Value);
                ent.Comp.DrawnTiles.Add(tile, spawned);
            }
        }

        foreach (var tile in ent.Comp.DrawnTiles)
        {
            if (!ent.Comp.Tiles.Contains(tile.Key))
                QueueDel(tile.Value);
        }
    }

    private void ShittyDrawDelete(Entity<FluidPoolComponent> ent)
    {
        foreach (var tile in ent.Comp.DrawnTiles)
        {
            QueueDel(tile.Value);
        }
        ent.Comp.DrawnTiles.Clear();
    }


    private bool GetTileCoords(Entity<FluidPoolComponent> ent, TileRef tile, [NotNullWhen(true)] out EntityCoordinates? coords)
    {
        coords = null;

        if (!TryComp<MapGridComponent>(tile.GridUid, out var gridComponent))
            return false;

        coords = _map.GridTileToLocal(ent.Comp.GridUid, gridComponent, tile.GridIndices);
        return true;
    }

    private EntityCoordinates GetTileCoords(Entity<MapGridComponent> ent, TileRef tile)
    {
        return _map.GridTileToLocal(ent.Owner, ent.Comp, tile.GridIndices);
    }

    /// <summary>
    /// Returns true if the pool is overflowing
    /// </summary>
    /// <param name="ent"></param>
    /// <returns></returns>
    private bool IsOverflowing(Entity<FluidPoolComponent> ent)
    {
        var volume = CurrentVolume(ent);
        var amountPerTile = (volume / ent.Comp.Tiles.Count);

        Log.Debug("Current volume for pool: " + ent.Owner + "  is: " + volume + "  Amount per tile is: " + amountPerTile);
        return amountPerTile > ent.Comp.OverflowVolume;
    }

    private FixedPoint2 CurrentVolume(Entity<FluidPoolComponent> ent)
    {
        return _solutionContainer.ResolveSolution(ent.Owner,
            ent.Comp.SolutionName,
            ref ent.Comp.Solution,
            out var solution)
            ? solution.Volume
            : FixedPoint2.Zero;
    }

    private void AddQueuedTiles(Entity<FluidPoolComponent> ent)
    {
        if (ent.Comp.AddedTiles.Count == 0)
            return;

        ent.Comp.Tiles.UnionWith(ent.Comp.AddedTiles);
        ent.Comp.AddedTiles.Clear();
        DirtyPool(ent);
    }

    private void RemoveTile(Entity<FluidPoolComponent> ent, TileRef tile)
    {
        ent.Comp.RemovedTiles.Add(tile);
    }

    private void RemoveTiles(Entity<FluidPoolComponent> ent, List<TileRef> tiles)
    {
        foreach (var tile in tiles)
        {
            ent.Comp.RemovedTiles.Add(tile);
        }
    }
}
