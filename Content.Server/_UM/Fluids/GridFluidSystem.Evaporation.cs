using System.Linq;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{
    private static readonly TimeSpan EvaporationCooldown = TimeSpan.FromSeconds(1);

    //Sometimes giant bodies of water will just disappear instantly
    private void ProcessEvaporation(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var gridFluid = ent.Comp1;

        var curTime = _timing.CurTime;

        if (gridFluid.NextTick > curTime)
            return;

        gridFluid.NextTick += EvaporationCooldown;

        foreach (var (indices, tile) in gridFluid.Tiles)
        {
            var solution = tile.Solution;

            var evaporationSpeeds = GetEvaporationSpeeds(tile.Solution);
            if (evaporationSpeeds.Count == 0)
                continue;

            var evaporationSpeed = evaporationSpeeds.Values.Sum() / evaporationSpeeds.Count;
            var reagentProportions = evaporationSpeeds.ToDictionary(kv => kv.Key,
                kv => solution.GetTotalPrototypeQuantity(kv.Key) / solution.Volume);

            // Still have to iterate over one-by-one since the full solution could have non-evaporating solutions.
            foreach (var (reagent, factor) in reagentProportions)
            {
                var reagentTick = gridFluid.EvaporationAmount * EvaporationCooldown.TotalSeconds * evaporationSpeed * factor;
                solution.SplitSolutionWithOnly(reagentTick, reagent);
            }

            if (solution.Volume == 0)
            {
                //TODO: not hard code this and shit
                Spawn("PuddleSparkle", _map.ToCoordinates(tile.GridIndex, tile.GridIndices));
                RemoveTile(gridFluid, tile);
                return;
            }

            AddActiveTile(gridFluid, indices);
            _gridFluidVisuals.MarkInvalid(ent.Owner, indices);
        }
    }

    /// <summary>
    /// Gets a mapping of evaporating speed of the reagents within a solution.
    /// The speed at which a solution evaporates is the average of the speed of all evaporating reagents in it.
    /// </summary>
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> GetEvaporationSpeeds(Solution solution)
    {
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> evaporatingSpeeds = [];
        foreach (var solProto in solution.GetReagentPrototypes(_prototypeManager).Keys)
        {
            if (solProto.EvaporationSpeed > FixedPoint2.Zero)
            {
                evaporatingSpeeds.Add(solProto.ID, solProto.EvaporationSpeed);
            }
        }
        return evaporatingSpeeds;
    }
}
