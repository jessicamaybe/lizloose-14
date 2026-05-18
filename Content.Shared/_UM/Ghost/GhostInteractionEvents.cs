using Content.Shared.Interaction;
using JetBrains.Annotations;

namespace Content.Shared._UM.Ghost;


/// <summary>
/// Raised on the user before interacting on an entity as a ghost
/// </summary>
public sealed class BeforeInteractGhostEvent : HandledEntityEventArgs
{
    public EntityUid Target { get; }

    public BeforeInteractGhostEvent(EntityUid target)
    {
        Target = target;
    }
}

[PublicAPI]
public sealed class InteractGhostEvent : HandledEntityEventArgs, ITargetedInteractEventArgs
{
    /// <summary>
    ///     Entity that triggered the interaction.
    /// </summary>
    public EntityUid User { get; }

    /// <summary>
    ///     Entity that was interacted on.
    /// </summary>
    public EntityUid Target { get; }

    public InteractGhostEvent(EntityUid user, EntityUid target)
    {
        User = user;
        Target = target;
    }
}
