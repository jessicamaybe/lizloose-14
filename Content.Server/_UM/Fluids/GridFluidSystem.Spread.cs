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

        var tileVolume = tile.Solution.Volume;

        var neighbors = new List<Vector2i>();

        if (tileVolume <= gridFluid.OverflowVolume)
        {
            RemoveActiveTile((ent.Owner, ent.Comp2, ent.Comp1), tile.GridIndices);
            return;
        }

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            if(!tile.BlockedDirections.IsFlagSet(direction))
                continue;
            var neighborPos = tile.GridIndices.Offset(direction);
            if (TryGetFluid(gridFluid, neighborPos, out var neighborTile)
                && neighborTile.Solution.Volume > tile.Solution.Volume + gridFluid.OverflowVolume)
                AddActiveTile(gridFluid, neighborTile);

            neighbors.Add(neighborPos);
        }

        if (neighbors.Count == 0)
        {
            RemoveActiveTile((ent.Owner, ent.Comp2, ent.Comp1), tile.GridIndices);
            return;
        }

        var flows = new List<(Vector2i pos, FixedPoint2 amount)>();

        foreach (var neighborIndices in neighbors)
        {
            FixedPoint2 neighborVolume = 0;

            if (TryGetFluid(gridFluid, neighborIndices, out var neighborTile))
                neighborVolume = neighborTile.Solution.Volume;

            if (neighborVolume >= tileVolume)
                continue;

            var diff = tileVolume - neighborVolume;

            if (diff < 0.1)
                continue;

            var flow = diff / 4;
            flows.Add((neighborIndices, flow));
        }

        foreach (var (pos, amount) in flows)
        {
            TryTransferFluid(gridFluid, tile, pos, amount);
        }

        if (tile.Solution.Volume <= gridFluid.OverflowVolume || flows.Count == 0)
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

            if (IsTileBlocked((ent.Owner, ent.Comp2, ent.Comp3), neighborPos))
                continue;

            neighboringTiles.Add(neighborPos);

        }
        return neighboringTiles;
    }

    /// <summary>
    /// Check if a tile is airtight or not
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="indices"></param>
    /// <returns></returns>
    private bool IsTileBlocked(Entity<MapGridComponent, TransformComponent> ent, Vector2i indices)
    {
        var xform = ent.Comp2;
        if (xform.GridUid == null)
            return true;

        var anchored = _map.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, ent.Comp1, indices);

        while (anchored.MoveNext(out var anchoredEnt))
        {
            if (_airtightQuery.TryGetComponent(anchoredEnt, out var airtightComponent) && airtightComponent.AirBlocked)
                return true;
        }

        return false;
    }
}
