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
    }

    private void UpdatePool(Entity<FluidPoolComponent> ent)
    {
        if (ent.Comp.Tiles.Count == 0)
        {
            Log.Debug("If this happens we fucked up");
            return;
        }

        if (!IsOverflowing(ent))
        {
            Log.Debug("pool is not overflowing");
            return;
        }

        if (ent.Comp.RoomFull)
        {
            ent.Comp.FillLevel += 1;
        }

        var neighborTiles = GetAvailableNeighbors(ent);
        if (neighborTiles.Count == 0)
        {
            //We're overflowing
            ent.Comp.RoomFull = true;
            return;
        }

        ent.Comp.RoomFull = false;
        ent.Comp.FillLevel = PoolFillLevel.Puddle;

        ent.Comp.AddedTiles.UnionWith(neighborTiles);
        DirtyPool(ent);
    }

    private void ShittyDraw(Entity<FluidPoolComponent> ent)
    {
        if (ent.Comp.FillLevel != ent.Comp.FillLevelLastRun)
        {
            foreach (var tile in ent.Comp.DrawnTiles)
            {
               QueueDel(tile.Value);
            }
            ent.Comp.DrawnTiles.Clear();
        }

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

    private bool GetTileCoords(Entity<FluidPoolComponent> ent, TileRef tile, [NotNullWhen(true)] out EntityCoordinates? coords)
    {
        coords = null;

        if (!TryComp<MapGridComponent>(tile.GridUid, out var gridComponent))
            return false;

        coords = _map.GridTileToLocal(ent.Comp.GridUid, gridComponent, tile.GridIndices);
        return true;
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

    private List<TileRef> GetAvailableNeighbors(Entity<FluidPoolComponent> ent)
    {
        var airtightQuery = GetEntityQuery<AirtightComponent>();

        List<TileRef> neighboringTiles = new();

        if (!TryComp<MapGridComponent>(ent.Comp.GridUid, out var gridComponent))
            return neighboringTiles;

        var gridXform = Transform(ent.Comp.GridUid);

        foreach (var tile in ent.Comp.EdgeTiles)
        {
            //Get neighboring tiles that aren't in our pool
            for (var i = 0; i < 4; i++)
            {
                var atmosDir = (AtmosDirection)(1 << i);
                var neighborPos = tile.GridIndices.Offset(atmosDir);
                if (!_map.TryGetTileRef(ent.Comp.GridUid, gridComponent, neighborPos, out var neighborTile))
                    continue;
                if (ent.Comp.Tiles.Contains(neighborTile))
                    continue;
                neighboringTiles.Add(neighborTile);
            }
        }
        List<TileRef> unblockedNeighbors = new();
        foreach (var tile in neighboringTiles)
        {
            if (IsTileBlocked((ent.Comp.GridUid, gridComponent, gridXform), airtightQuery, tile))
                continue;
            unblockedNeighbors.Add(tile);
        }

        return unblockedNeighbors;
    }

    private HashSet<TileRef> GetEdgeTiles(Entity<FluidPoolComponent> ent)
    {
        var edgeTiles = new HashSet<TileRef>();

        if (!TryComp<MapGridComponent>(ent.Comp.GridUid, out var gridComponent))
            return edgeTiles;

        if (ent.Comp.Tiles.Count == 1)
            ent.Comp.EdgeTiles = ent.Comp.Tiles;

        foreach (var tile in ent.Comp.Tiles)
        {
            for (var i = 0; i < 4; i++)
            {
                var atmosDir = (AtmosDirection)(1 << i);
                var neighborPos = tile.GridIndices.Offset(atmosDir);

                if (!_map.TryGetTileRef(ent.Comp.GridUid, gridComponent, neighborPos, out var neighborTile))
                {
                    edgeTiles.Add(tile);
                    break;
                }

                if (!ent.Comp.Tiles.Contains(neighborTile))
                {
                    edgeTiles.Add(tile);
                    break;
                }
            }
        }
        return edgeTiles;
    }

    public bool IsTileBlocked(Entity<MapGridComponent, TransformComponent> ent, EntityQuery<AirtightComponent> airtightQuery, TileRef tileRef)
    {
        var xform = ent.Comp2;
        if (xform.GridUid == null)
            return true;

        var anchored = _map.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, ent.Comp1, tileRef.GridIndices);

        //if (_turf.IsSpace(tileRef))
        //    return true;

        while (anchored.MoveNext(out var anchoredEnt))
        {
            if (airtightQuery.TryGetComponent(anchoredEnt, out var airtightComponent) && airtightComponent.AirBlocked)
                return true;
        }

        return false;
    }
}
