using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared._UM.Puddles;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._UM.Destructable.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpecialSpillBehavior : IThresholdBehavior
{
    /// <summary>
    /// Optional fallback solution name if SpillableComponent is not present.
    /// </summary>
    [DataField]
    public string? Solution;

    [DataField]
    public EntProtoId ProtoId;

    /// <summary>
    /// When triggered, spills the entity's solution onto the ground.
    /// Will first try to use the solution from a SpillableComponent if present,
    /// otherwise falls back to the solution specified in the behavior's data fields.
    /// The solution is properly drained/split before spilling to prevent double-spilling with other behaviors.
    /// </summary>
    /// <param name="owner">Entity whose solution will be spilled</param>
    /// <param name="system">System calling this behavior</param>
    /// <param name="cause">Optional entity that caused this behavior to trigger</param>
    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        var puddleSystem = system.EntityManager.System<SpecialPuddleSystem>();
        var solutionContainer = system.EntityManager.System<SharedSolutionContainerSystem>();

        // Spill the solution that was drained/split
        if (solutionContainer.TryGetSolution(owner, Solution, out _, out var solution))
        {
            puddleSystem.TrySpillAt(owner, solution, ProtoId);
        }
    }
}
