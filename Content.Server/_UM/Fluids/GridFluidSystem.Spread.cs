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

            var diff = tileVolume - neighborVolume;

            if (diff <= 0)
                continue;

            // pairwise leveling
            var flow = diff / 4;

            if (flow < 0.1)
                continue;

            flows.Add((neighborIndices, flow));
        }
        FixedPoint2 totalRequested = 0;

        foreach (var flow in flows)
        {
            totalRequested += flow.amount;
        }

        var maxOut = tileVolume - gridFluid.OverflowVolume;

        if (totalRequested > maxOut && totalRequested > 0)
        {
            var scale = maxOut / totalRequested;

            for (var i = 0; i < flows.Count; i++)
            {
                var (pos, amt) = flows[i];
                flows[i] = (pos, amt * scale);
            }
        }

        var moved = false;
        foreach (var (pos, amount) in flows)
        {
            if (amount == 0)
                continue;

            TryTransferFluid(gridFluid, tile, pos, amount);
            _gridFluidVisuals.MarkInvalid((ent.Owner, gridFluid), pos);
            moved = true;
        }

        if (!moved)
            return;

        _gridFluidVisuals.MarkInvalid((ent.Owner, gridFluid), tile.GridIndices);
        RemoveActiveTile((ent.Owner, ent.Comp2, ent.Comp1), tile.GridIndices);

        if (tile.Solution.Volume <= gridFluid.OverflowVolume || flows.Count == 0)
            RemoveActiveTile((ent.Owner, ent.Comp2, ent.Comp1), tile.GridIndices);
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
        var tileRef = _map.GetTileRef((ent.Owner, ent.Comp1), indices);

        if (_turf.IsSpace(tileRef))
            return true;

        while (anchored.MoveNext(out var anchoredEnt))
        {
            if (_airtightQuery.TryGetComponent(anchoredEnt, out var airtightComponent) && airtightComponent.AirBlocked)
                return true;
        }

        return false;
    }
}
