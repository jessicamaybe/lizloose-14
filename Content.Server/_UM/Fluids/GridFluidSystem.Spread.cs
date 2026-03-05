using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{

    private void ProcessFluidSpread(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent,
        TileSolution tile)
    {
        var (owner, gridFluid, grid, xform) = ent;

        if (tile.Solution.Volume <= gridFluid.OverflowVolume)
        {
            RemoveActiveTile((ent.Owner, ent.Comp2, ent.Comp1), tile.GridIndices);
            return;
        }

        var neighbors = GetAvailableNeighbors(ent, tile.GridIndices, tile);

        if (neighbors.Count == 0)
        {
            RemoveActiveTile((ent.Owner, ent.Comp2, ent.Comp1), tile.GridIndices);
            return;
        }

        var goodNeighborTiles = new List<Vector2i>();

        foreach (var neighborIndices in neighbors)
        {
            if (!TryGetFluid((ent.Owner, ent.Comp2, ent.Comp1), neighborIndices, out var neighborTile))
            {
                goodNeighborTiles.Add(neighborIndices);
                continue;
            }

            if (tile.TileSolutionGroup != null && neighborTile.TileSolutionGroup != null)
            {
                if (neighborTile.TileSolutionGroup != tile.TileSolutionGroup)
                {
                    ExcitedGroupMerge(gridFluid, tile.TileSolutionGroup, neighborTile.TileSolutionGroup);
                }
            }
            var tileGroup = tile.TileSolutionGroup;
            tileGroup ??= tile.TileSolutionGroup;

            if (tileGroup == null)
            {
                tileGroup = new TileSolutionGroup();
                gridFluid.TileGroups.Add(tileGroup);
            }
            if (tile.TileSolutionGroup == null)
                TileGroupAddTile(tileGroup, tile);
            if (neighborTile.TileSolutionGroup == null)
                TileGroupAddTile(tileGroup, neighborTile);

            if (neighborTile.Solution.Volume >= tile.Solution.Volume)
                continue;

            goodNeighborTiles.Add(neighborIndices);
        }

        if (goodNeighborTiles.Count == 0)
        {
            return;
        }

        var splitVolume = (tile.Solution.Volume - gridFluid.OverflowVolume) / (goodNeighborTiles.Count + 1);

        tile.ShareVolume = tile.Solution.Volume - splitVolume;

        foreach (var neighbor in goodNeighborTiles)
        {
            var overflowSolution = tile.Solution.SplitSolution(splitVolume);
            AddFluid((owner, grid), neighbor, overflowSolution);
        }

        if (tile.Solution.Volume > gridFluid.OverflowVolume)
            AddActiveTile((ent.Owner, ent.Comp2, ent.Comp1), tile.GridIndices);

        if (tile.Solution.Volume <= gridFluid.OverflowVolume)
            RemoveActiveTile((ent.Owner, ent.Comp2, ent.Comp1), tile.GridIndices);
    }

    /// <summary>
    /// Gets neighbors of a tile
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="indices"></param>
    /// <param name="tile"></param>
    /// <returns></returns>
    private List<Vector2i> GetAvailableNeighbors(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent, Vector2i indices, TileSolution tile)
    {
        var (owner, gridFluid, grid, xform) = ent;

        List<Vector2i> neighboringTiles = new();

        for (var i = 0; i < 4; i++)
        {
            var atmosDir = (AtmosDirection)(1 << i);

            var neighborPos = tile.GridIndices.Offset(atmosDir);

            if (IsTileBlocked((ent.Owner, ent.Comp2, ent.Comp3), _airtightQuery, neighborPos))
                continue;

            if (TryGetFluid((owner, grid, gridFluid), neighborPos, out var fluid)
                && tile.Solution.Volume + gridFluid.OverflowVolume < fluid.Solution.Volume)
                continue;

            neighboringTiles.Add(neighborPos);

        }
        return neighboringTiles;
    }


    /// <summary>
    /// Check if a tile is airtight or not
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="airtightQuery"></param>
    /// <param name="indices"></param>
    /// <returns></returns>
    private bool IsTileBlocked(Entity<MapGridComponent, TransformComponent> ent, EntityQuery<AirtightComponent> airtightQuery, Vector2i indices)
    {
        var xform = ent.Comp2;
        if (xform.GridUid == null)
            return true;

        var anchored = _map.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, ent.Comp1, indices);

        while (anchored.MoveNext(out var anchoredEnt))
        {
            if (airtightQuery.TryGetComponent(anchoredEnt, out var airtightComponent) && airtightComponent.AirBlocked)
                return true;
        }

        return false;
    }
}
