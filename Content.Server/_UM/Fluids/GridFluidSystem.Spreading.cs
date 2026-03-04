using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem
{
    private void SpreadPool(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent, Entity<FluidPoolComponent> pool)
    {
        var neighborTiles = GetAvailableNeighbors(ent, pool);

        if (neighborTiles.Count == 0)
        {
            pool.Comp.RoomFull = true;
            return;
        }

        foreach (var tile in neighborTiles)
        {
            if (IsTilePool((ent.Owner, ent.Comp2, ent.Comp3), tile, out var mergePool)
                && !ent.Comp1.DeletedPools.Contains(mergePool.Value)
                && !ent.Comp1.Mergers.ContainsKey(tile))
            {
                //Add to merge queue
                StartMerge(ent, pool, mergePool.Value, tile);
                Log.Debug("this tile is already a pool, starting to merge these puddles");
                continue;
            }

            pool.Comp.AddedTiles.Add(tile);
        }
        DirtyPool(pool);
    }

    /// <summary>
    /// Returns list of neighbors that this pool is allowed to spread onto
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="pool"></param>
    /// <returns></returns>
    private List<TileRef> GetAvailableNeighbors(Entity<GridFluidComponent, MapGridComponent, TransformComponent> grid, Entity<FluidPoolComponent> pool)
    {
        var airtightQuery = GetEntityQuery<AirtightComponent>();

        List<TileRef> neighboringTiles = new();

        foreach (var tile in pool.Comp.EdgeTiles)
        {
            //Get neighboring tiles that aren't in our pool
            for (var i = 0; i < 4; i++)
            {
                var atmosDir = (AtmosDirection)(1 << i);
                var neighborPos = tile.GridIndices.Offset(atmosDir);
                if (!_map.TryGetTileRef(pool.Comp.GridUid, grid.Comp2, neighborPos, out var neighborTile))
                    continue;
                if (pool.Comp.Tiles.Contains(neighborTile))
                    continue;
                if (IsTileBlocked((pool.Comp.GridUid, grid.Comp2, grid.Comp3), airtightQuery, neighborTile))
                    continue;
                neighboringTiles.Add(neighborTile);
            }
        }
        return neighboringTiles;
    }

    /// <summary>
    /// Returns tiles that are on the edge of a puddle
    /// </summary>
    /// <param name="ent"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Checks if a tileref belongs to an existing puddle
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="tile"></param>
    /// <param name="pool"></param>
    /// <returns></returns>
    private bool IsTilePool(Entity<MapGridComponent, TransformComponent> ent, TileRef tile, [NotNullWhen(true)] out Entity<FluidPoolComponent>? pool)
    {
        pool = null;
        if (TryGetPool(ent, _map.GridTileToLocal(ent.Owner, ent.Comp1, tile.GridIndices), out pool))
            return true;
        return false;
    }

    /// <summary>
    /// Check if a tile is airtight or not
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="airtightQuery"></param>
    /// <param name="tileRef"></param>
    /// <returns></returns>
    private bool IsTileBlocked(Entity<MapGridComponent, TransformComponent> ent, EntityQuery<AirtightComponent> airtightQuery, TileRef tileRef)
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
