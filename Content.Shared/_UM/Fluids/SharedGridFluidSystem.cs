using System.Diagnostics.CodeAnalysis;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.Components;

namespace Content.Shared._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class SharedGridFluidSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    public bool TryGetSolution(Entity<TileSolutionRelayComponent?> ent, [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.TileSolution == null)
            return false;

        solution = ent.Comp.TileSolution.Solution;

        Log.Debug("Trying to get solution: " + solution.Volume);
        return true;
    }
}
