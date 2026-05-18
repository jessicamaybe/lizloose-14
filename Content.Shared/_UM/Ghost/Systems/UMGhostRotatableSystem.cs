using Content.Shared._UM.Ghost.Components;
using Content.Shared.Rotatable;

namespace Content.Shared._UM.Ghost.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class UMGhostRotatableSystem : EntitySystem
{
    [Dependency] private readonly RotatableSystem _rotatableSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UMGhostRotatableComponent, InteractGhostEvent>(OnActivateInWorld);
    }


    private void OnActivateInWorld(Entity<UMGhostRotatableComponent> ent, ref InteractGhostEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RotatableComponent>(ent, out var rotatableComp))
            return;

        _rotatableSystem.Rotate(args.Target, rotatableComp.Increment);
    }
}
