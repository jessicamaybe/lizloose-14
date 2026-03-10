using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{
    public override void DirtyTile(Entity<TileFluidComponent> ent)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } grid)
            return;

        _gridFluidVisuals.MarkInvalid(grid, ent.Comp.Indices);
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
