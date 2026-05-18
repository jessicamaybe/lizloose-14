using Content.Shared.Ghost;

namespace Content.Shared._UM.Ghost.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class UMGhostInteractionSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    public bool CanInteract(Entity<GhostComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return true;
    }

    public void Interact(Entity<GhostComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ev = new BeforeInteractGhostEvent(target);
        RaiseLocalEvent(ent, ev);

        if (ev.Handled)
            return;

        var message = new InteractGhostEvent(ent, target);
        RaiseLocalEvent(target, message, true);
    }
}
