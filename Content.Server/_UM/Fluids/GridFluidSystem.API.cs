using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{
    /// <summary>
    /// Invalidates a tile on the grid, marked tiles will have their available neighbors reevaluated next update cycle.
    /// If the tile does not exist in a current body of fluid, will check if its neighbors does, and invalidate them instead.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="indices"></param>
    [PublicAPI]
    public void InvalidateTile(Entity<GridFluidComponent?> ent, Vector2i indices)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!ent.Comp.Tiles.TryGetValue(indices, out var tile))
        {
            for (var i = 0; i < 4; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var neighborPos = indices.Offset(direction);
                if (TryGetFluid(ent.Comp, neighborPos, out var neighbor))
                    InvalidateTile(ent.Comp, neighbor);
            }
            return;
        }
        ent.Comp.InvalidTiles.Add(tile);
    }

}
