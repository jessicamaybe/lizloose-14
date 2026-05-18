using Content.Shared._UM.Ghost.Components;
using Content.Shared.Follower;

namespace Content.Shared._UM.Ghost.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class UMGhostGroupInteractableSystem : EntitySystem
{
    [Dependency] private readonly FollowerSystem _followerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UMGhostGroupInteractableComponent, InteractGhostEvent>(OnGhostInteract);
        SubscribeLocalEvent<UMGhostGroupInteractableComponent, EntityStartedFollowingEvent>(OnStartFollowing);
        SubscribeLocalEvent<UMGhostGroupInteractableComponent, EntityStoppedFollowingEvent>(OnStopFollowing);
    }

    private void OnStartFollowing(Entity<UMGhostGroupInteractableComponent> ent, ref EntityStartedFollowingEvent args)
    {
        if (ent.Comp.CurrentGhosts.Count >= ent.Comp.Amount )
            Log.Debug("We win");


    }

    private void OnStopFollowing(Entity<UMGhostGroupInteractableComponent> ent, ref EntityStoppedFollowingEvent args)
    {
        if (ent.Comp.CurrentGhosts.Contains(args.Follower))
            ent.Comp.CurrentGhosts.Remove(args.Follower);

        Dirty(ent);
    }


    private void OnGhostInteract(Entity<UMGhostGroupInteractableComponent> ent, ref InteractGhostEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.CurrentGhosts.Contains(args.User))
            return;

        ent.Comp.CurrentGhosts.Add(args.User);
        _followerSystem.StartFollowingEntity(args.User, ent);
        Dirty(ent);
    }
}
