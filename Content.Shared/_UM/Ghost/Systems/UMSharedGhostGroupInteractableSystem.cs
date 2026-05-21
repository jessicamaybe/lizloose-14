using Content.Shared._UM.Ghost.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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

        SubscribeAllEvent<RequestGhostActionVoteEvent>(HandleGhostActionVote);

        SubscribeLocalEvent<DoorComponent, GetGhostGroupActionsEvent>(OnGetGhostActions);
    }

    private void HandleGhostActionVote(RequestGhostActionVoteEvent ev, EntitySessionEventArgs eventArgs)
    {
        var user = eventArgs.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryGetEntity(ev.Target, out var target))
            return;

        if (Deleted(user))
            return;

        if (!HasComp<GhostComponent>(user))
            return;

        var actions = GetGhostActions(target.Value, user.Value);

        if (!actions.TryGetValue(ev.RequestedAction, out var action))
            return;

        if (!TryComp<UMGhostGroupInteractableComponent>(target, out var comp))
            return;

        comp.CurrentGhosts.Add(user.Value);

        comp.Votes.Add(user.Value, action);

        _followerSystem.StartFollowingEntity(user.Value, target.Value);
        Dirty(target.Value, comp);
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
    private void OnStopFollowing(Entity<UMGhostGroupInteractableComponent> ent, ref EntityStoppedFollowingEvent args)
    {
        if (ent.Comp.CurrentGhosts.Contains(args.Follower))
            ent.Comp.CurrentGhosts.Remove(args.Follower);

        ent.Comp.Votes.Remove(args.Follower);
        Dirty(ent);
    }

    public SortedSet<GhostGroupAction> GetGhostActions(EntityUid target, EntityUid user)
    {
        SortedSet<GhostGroupAction> actions = new();

        if (!HasComp<GhostComponent>(user))
            return actions;

        var ev = new GetGhostGroupActionsEvent(user, target);
        RaiseLocalEvent(target, ev);

        actions.UnionWith(ev.Actions);

        return actions;
    }

    private void OnGhostInteract(Entity<UMGhostGroupInteractableComponent> ent, ref InteractGhostEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.CurrentGhosts.Contains(args.User))
            return;


        var actions = GetGhostActions(args.Target, args.User);
        var ev = new GhostActionMenuRequestEvent(GetNetEntity(ent), actions);
        RaiseLocalEvent(ev);


        //ent.Comp.CurrentGhosts.Add(args.User);
        //_followerSystem.StartFollowingEntity(args.User, ent);
        //Dirty(ent);
    }

    private void OpenDoor(Entity<DoorComponent> ent)
    {
        if (_door.IsBolted(ent) && TryComp<DoorBoltComponent>(ent, out var doorBolts))
        {
            _door.SetBoltsDown((ent, doorBolts), false);
        }
        _door.StartOpening(ent);
    }

    private void OnGetGhostActions(Entity<DoorComponent> ent, ref GetGhostGroupActionsEvent args)
    {
        var open = new GhostGroupAction()
        {
            Act = () => OpenDoor(ent),
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Structures/Doors/Airlocks/Standard/basic.rsi"), "assembly"),
            Priority = 0,
            Color = Color.Red
        };
        args.Actions.Add(open);

        if (TryComp<DoorBoltComponent>(ent, out var doorBolt))
        {
            var bolt = new GhostGroupAction()
            {
                Act = () => _door.TrySetBoltDown((ent, doorBolt), true),
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Actions/actions_ai.rsi"), "bolt_door"),
                Priority = 1,
                Color = Color.Green
            };
            args.Actions.Add(bolt);
        }
    }
}
