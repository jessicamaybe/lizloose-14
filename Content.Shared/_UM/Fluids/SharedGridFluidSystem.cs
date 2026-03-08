using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SharedGridFluidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializeExamine();
    }

    public void AddFluid(Entity<TileFluidComponent?> ent, Solution solution)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Solution == null)
            return;

        ent.Comp.Solution.AddSolution(solution, _prototype);
        DirtyTile((ent, ent.Comp));
    }

    public virtual void DirtyTile(Entity<TileFluidComponent> ent)
    {
    }

    public bool TryGetSolution(Entity<TileFluidComponent?> ent, [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Solution == null)
            return false;

        solution = ent.Comp.Solution;
        Dirty(ent);
        return true;
    }

}
