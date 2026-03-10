using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{
    private bool TryTransferFluid(GridFluidComponent gridFluid, Vector2i indicesFrom, Vector2i indicesTo, FixedPoint2 amount, bool active = true)
    {
        if (!gridFluid.Tiles.TryGetValue(indicesFrom, out var tileFrom))
            return false;

        return TryTransferFluid(gridFluid, tileFrom, indicesTo, amount, active);
    }

    private bool TryTransferFluid(GridFluidComponent gridFluid, TileSolution tileFrom, Vector2i indicesTo, FixedPoint2 amount, bool active = true)
    {
        var solution = tileFrom.Solution.SplitSolution(amount);

        if (gridFluid.Tiles.TryGetValue(indicesTo, out var tileTo))
        {
            tileTo.Solution.AddSolution(solution, _prototypeManager);
            AddTileReaction(gridFluid, tileTo);
            if (active)
                AddActiveTile(gridFluid, indicesTo);
            return true;
        }

        AddFluid((tileFrom.GridIndex, gridFluid), indicesTo, solution, active);
        return true;
    }

    /// <summary>
    /// Adds a solution to a tile
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="coords"></param>
    /// <param name="solution"></param>
    public void AddFluid(Entity<MapGridComponent> ent, EntityCoordinates coords, Solution solution)
    {
        if (!_map.TryGetTileRef(ent.Owner, ent.Comp, coords, out var tileRef))
            return;

        var gridFluid = EnsureComp<GridFluidComponent>(ent.Owner);

        AddFluid((ent.Owner, gridFluid), tileRef.GridIndices, solution, true);
    }

    public void AddFluid(Entity<GridFluidComponent> ent, Vector2i indices, Solution solution, bool active = true)
    {
        if (ent.Comp.Tiles.TryGetValue(indices, out var tile))
        {
            tile.Solution.AddSolution(solution, _prototypeManager);
            AddTileReaction(ent.Comp, tile);
            MarkModifiedTile(ent.Comp, indices);
            if (active)
                AddActiveTile(ent.Comp, indices);
            return;
        }

        AddTile((ent.Owner, ent.Comp), indices, solution, active);
        MarkModifiedTile(ent.Comp, indices);
    }

    public void MarkModifiedTile(GridFluidComponent gridFluid, Vector2i indices)
    {
        gridFluid.ModifiedTiles.Add(indices);
    }
}
