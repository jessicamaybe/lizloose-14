using System.Linq;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;
using Robust.Shared.Spawners;

namespace Content.Server._UM.Fluids;

public sealed partial class GridFluidSystem
{
    //The great big file we copy/paste from ChemicalReactionSystem so we can deal with entity effects

    private const int MaxReactionIterations = 20;

    /// <summary>
    ///     Continually react a solution until no more reactions occur, with a volume constraint.
    /// </summary>
    private void FullyReactSolution(GridFluidComponent gridFluid, TileSolution tileSolution, ReactionMixerComponent? mixerComponent = null)
    {
        // construct the initial set of reactions to check.
        SortedSet<ReactionPrototype> reactions = new();
        foreach (var reactant in tileSolution.Solution.Contents)
        {
            if (_solutionReaction._reactionsSingle.TryGetValue(reactant.Reagent.Prototype, out var reactantReactions))
                reactions.UnionWith(reactantReactions);
        }

        // Repeatedly attempt to perform reactions, ending when there are no more applicable reactions, or when we
        // exceed the iteration limit.
        for (var i = 0; i < MaxReactionIterations; i++)
        {
            if (!ProcessReactions(gridFluid, tileSolution, reactions, mixerComponent))
                return;
        }

        //Log.Error($"{nameof(Solution)} {soln.Owner} could not finish reacting in under {MaxReactionIterations} loops.");
    }

    /// <summary>
    ///     Checks if a solution can undergo a specified reaction.
    /// </summary>
    private bool CanReact(TileSolution tileSolution,
        ReactionPrototype reaction,
        ReactionMixerComponent? mixerComponent,
        out FixedPoint2 lowestUnitReactions)
    {
        var solution = tileSolution.Solution;

        lowestUnitReactions = FixedPoint2.MaxValue;
        if (solution.Temperature < reaction.MinimumTemperature)
        {
            lowestUnitReactions = FixedPoint2.Zero;
            return false;
        }

        if (solution.Temperature > reaction.MaximumTemperature)
        {
            lowestUnitReactions = FixedPoint2.Zero;
            return false;
        }

        if ((mixerComponent == null && reaction.MixingCategories != null) ||
            mixerComponent != null && reaction.MixingCategories != null &&
            reaction.MixingCategories.Except(mixerComponent.ReactionTypes).Any())
        {
            lowestUnitReactions = FixedPoint2.Zero;
            return false;
        }

        foreach (var reactantData in reaction.Reactants)
        {
            var reactantName = reactantData.Key;
            var reactantCoefficient = reactantData.Value.Amount;

            var reactantQuantity = solution.GetTotalPrototypeQuantity(reactantName);

            if (reactantQuantity <= FixedPoint2.Zero)
                return false;

            if (reactantData.Value.Catalyst)
            {
                // catalyst is not consumed, so will not limit the reaction. But it still needs to be present, and
                // for quantized reactions we need to have a minimum amount

                if (reactantQuantity == FixedPoint2.Zero ||
                    reaction.Quantized && reactantQuantity < reactantCoefficient)
                    return false;

                continue;
            }

            var unitReactions = reactantQuantity / reactantCoefficient;

            if (unitReactions < lowestUnitReactions)
            {
                lowestUnitReactions = unitReactions;
            }
        }

        if (reaction.Quantized)
            lowestUnitReactions = (int)lowestUnitReactions;

        return lowestUnitReactions > 0;
    }


    /// <summary>
    ///     Performs all chemical reactions that can be run on a solution.
    ///     Removes the reactants from the solution, then returns a solution with all products.
    ///     WARNING: Does not trigger reactions between solution and new products.
    /// </summary>
    private bool ProcessReactions(GridFluidComponent gridFluid,
        TileSolution tileSolution,
        SortedSet<ReactionPrototype> reactions,
        ReactionMixerComponent? mixerComponent)
    {
        List<string>? products = null;

        // attempt to perform any applicable reaction
        foreach (var reaction in reactions)
        {
            if (!CanReact(tileSolution, reaction, mixerComponent, out var unitReactions))
            {
                continue;
            }

            products = PerformReaction(gridFluid, tileSolution, reaction, unitReactions);
            break;
        }

        // did any reaction occur?
        if (products == null)
            return false;

        if (products.Count == 0)
            return true;

        // Add any reactions associated with the new products. This may re-add reactions that were already iterated
        // over previously. The new product may mean the reactions are applicable again and need to be processed.
        foreach (var product in products)
        {
            if (_solutionReaction._reactions.TryGetValue(product, out var reactantReactions))
                reactions.UnionWith(reactantReactions);
        }
        _gridFluidVisuals.MarkInvalid((tileSolution.GridIndex, gridFluid), tileSolution.GridIndices);
        return true;
    }

    /// <summary>
    ///     Perform a reaction on a solution. This assumes all reaction criteria are met.
    ///     Removes the reactants from the solution, adds products, and returns a list of products.
    /// </summary>
    private List<string> PerformReaction(GridFluidComponent gridFluid, TileSolution tileSolution, ReactionPrototype reaction, FixedPoint2 unitReactions)
    {
        var solution = tileSolution.Solution;

        var energy = reaction.ConserveEnergy ? solution.GetThermalEnergy(_prototypeManager) : 0;

        //Remove reactants
        foreach (var reactant in reaction.Reactants)
        {
            if (!reactant.Value.Catalyst)
            {
                var amountToRemove = unitReactions * reactant.Value.Amount;
                solution.RemoveReagent(reactant.Key, amountToRemove, ignoreReagentData: true);
            }
        }

        //Create products
        var products = new List<string>();
        foreach (var product in reaction.Products)
        {
            products.Add(product.Key);
            solution.AddReagent(product.Key, product.Value * unitReactions);
        }

        if (reaction.ConserveEnergy)
        {
            var newCap = solution.GetHeatCapacity(_prototypeManager);
            if (newCap > 0)
                solution.Temperature = energy / newCap;
        }

        OnReaction(gridFluid, tileSolution, reaction, null, unitReactions);

        return products;
    }

    private void OnReaction(GridFluidComponent gridFluid, TileSolution tileSolution, ReactionPrototype reaction, ReagentPrototype? reagent, FixedPoint2 unitReactions)
    {
        if (!TryComp<MapGridComponent>(tileSolution.GridIndex, out var mapGrid))
            return;

        var coords = _map.GridTileToLocal(tileSolution.GridIndex, mapGrid, tileSolution.GridIndices);
        var entity = SpawnAtPosition(null, coords);

        var solutionComp = EnsureComp<TileFluidComponent>(entity);
        solutionComp.Solution = tileSolution.Solution;

        var timedDespawn = EnsureComp<TimedDespawnComponent>(entity);
        timedDespawn.Lifetime = 10f;
        _entityEffects.ApplyEffects(entity, reaction.Effects, unitReactions);
    }

}
