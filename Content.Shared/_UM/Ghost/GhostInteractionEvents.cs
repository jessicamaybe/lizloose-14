using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

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

public sealed class GetGhostGroupActionsEvent : EntityEventArgs
{
    public readonly SortedSet<GhostGroupAction> Actions = new();

    public readonly EntityUid Target;

    public readonly EntityUid User;

    public GetGhostGroupActionsEvent(EntityUid user, EntityUid target)
    {
        Target = target;
        User = user;
    }
}

[Serializable, NetSerializable]
public sealed class GhostActionMenuRequestEvent : EntityEventArgs
{
    public readonly List<GhostGroupAction>? Actions;

    public readonly NetEntity Entity;

    public GhostActionMenuRequestEvent(NetEntity entity, SortedSet<GhostGroupAction>? actions)
    {
        Entity = entity;

        if (actions == null)
            return;

        Actions = new(actions);
    }
}


[Serializable, NetSerializable]
public sealed class RequestGhostActionVoteEvent : EntityEventArgs
{
    public readonly NetEntity Target;
    public readonly GhostGroupAction RequestedAction;

    public RequestGhostActionVoteEvent(NetEntity target, GhostGroupAction requestedAction)
    {
        Target = target;
        RequestedAction = requestedAction;
    }
}

