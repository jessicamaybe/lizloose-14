using System.Diagnostics.CodeAnalysis;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{
    public override void DirtyTile(Entity<TileFluidComponent> ent)
    {
        if (ent.Comp.TileSolution == null)
            return;

        InvalidateTile(ent.Comp.TileSolution.GridIndex, ent.Comp.TileSolution.GridIndices);
        _gridFluidVisuals.MarkInvalid(ent.Comp.TileSolution.GridIndex, ent.Comp.TileSolution.GridIndices);
    }

    /// <summary>
    /// Tries to get the solution on a given tile
    /// </summary>
    /// <param name="gridUid"></param>
    /// <param name="tileRef"></param>
    /// <param name="tileSolution"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryGetTileSolution(EntityUid gridUid, TileRef tileRef, [NotNullWhen(true)] out TileSolution? tileSolution)
    {
        tileSolution = null;

        if (!TryComp<GridFluidComponent>(gridUid, out var gridFluidComponent))
            return false;

        if (!TryGetFluid(gridFluidComponent, tileRef.GridIndices, out var tile))
            return false;

        tileSolution = tile;
        return true;
    }

    [PublicAPI]
    public bool TryGetTileSolution(GridFluidComponent gridFluid, Vector2i indices, [NotNullWhen(true)] out TileSolution? tileSolution)
    {
        tileSolution = null;

        if (!TryGetFluid(gridFluid, indices, out var tile))
            return false;

        tileSolution = tile;
        return true;
    }

    /// <summary>
    /// Whenever a tile solution is changed, it needs to be marked active to update.
    /// </summary>
    /// <param name="gridUid"></param>
    /// <param name="tileRef"></param>
    /// <returns></returns>
    public bool MarkActiveTileSolution(EntityUid gridUid, TileRef tileRef)
    {
        if (!TryComp<GridFluidComponent>(gridUid, out var gridFluidComponent))
            return false;

        return AddActiveTile(gridFluidComponent, tileRef.GridIndices);
    }

    /// <summary>
    /// Invalidates a tile on the grid
    /// marked tiles will have themselves and neighbors reevaluated next update cycle.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="indices"></param>
    [PublicAPI]
    public void InvalidateTile(Entity<GridFluidComponent?> ent, Vector2i indices)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (TryGetFluid(ent.Comp, indices, out var tile))
            ent.Comp.InvalidTiles.Add(tile);

        for (var i = 0; i < 4; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            var neighborPos = indices.Offset(direction);
            if (TryGetFluid(ent.Comp, neighborPos, out var neighbor))
                InvalidateTile(ent.Comp, neighbor);
        }
    }

}
