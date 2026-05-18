using Content.Shared._UM.Ghost.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Follower;
using Robust.Shared.Timing;

namespace Content.Shared._UM.Ghost.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class UMGhostGroupInteractableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;

    [Dependency] private readonly SharedDoorSystem _door = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UMGhostGroupInteractableComponent, InteractGhostEvent>(OnGhostInteract);
        SubscribeLocalEvent<UMGhostGroupInteractableComponent, EntityStoppedFollowingEvent>(OnStopFollowing);

        SubscribeLocalEvent<DoorComponent, GhostGroupInteractEvent>(OnDoorGhost);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<UMGhostGroupInteractableComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentGhosts.Count < comp.Amount )
                continue;

            if (comp.LastActivated + comp.Cooldown < _timing.CurTime)
            {
                var ev = new GhostGroupInteractEvent();
                RaiseLocalEvent(uid, ref ev);

                comp.LastActivated = _timing.CurTime;
                Dirty(uid, comp);
            }
        }
    }

    private void OnDoorGhost(Entity<DoorComponent> ent, ref GhostGroupInteractEvent args)
    {
        if (_door.IsBolted(ent) && TryComp<DoorBoltComponent>(ent, out var doorBolts))
        {
            _door.SetBoltsDown((ent, doorBolts), false);
        }
        _door.StartOpening(ent);
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
