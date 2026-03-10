using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.FixedPoint;

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
            MarkModifiedTile(gridFluid, indicesTo);
            AddTileReaction(gridFluid, tileTo);
            if (active)
                AddActiveTile(gridFluid, indicesTo);
            return true;
        }

        AddFluid((tileFrom.GridIndex, gridFluid), indicesTo, solution, active);
        return true;
    }
}
