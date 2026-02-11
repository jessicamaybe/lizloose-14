using Content.Server.Database;
using Content.Server.Shuttles.Systems;
using Content.Shared._UM.Drip;
using Content.Shared._UM.Drip.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Trigger.Systems;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Server._UM.Drip;

/// <inheritdoc/>
public sealed class TrackedDripSystem : SharedTrackedDripSystem
{
    [Dependency] private readonly EmergencyShuttleSystem _eShuttle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        var dripQuery = EntityQueryEnumerator<TrackedDripComponent>();

        // trigger everything with the component
        while (dripQuery.MoveNext(out var uid, out var comp))
        {
            TrackTheDrip((uid, comp));
        }
    }

    private void TrackTheDrip(Entity<TrackedDripComponent> ent)
    {
        var prototype = MetaData(ent.Owner).EntityPrototype;
        if (prototype == null)
            return;

        if (!_container.TryGetOuterContainer(ent, Transform(ent), out var container))
            return;

        var player = container.Owner;

        if (!_playerManager.TryGetSessionByEntity(player, out var session))
            return;

        if (!TryComp<MobStateComponent>(player, out var mobState) || mobState.CurrentState == MobState.Dead)
            return;

        /* shuttle check
        var shuttle = _eShuttle.GetShuttle();
        if (shuttle == null)
            return;
        if (Transform(shuttle.Value).MapID != _xform.GetMapCoordinates(player).MapId)
            return;
        */
        Log.Debug("Succecssful drip check");
        RecordDrip(session.UserId, prototype.ID, ent.Comp.Rounds);
    }

    private void RecordDrip(NetUserId playerId, string drip, int rounds)
    {
        _db.UpdateDrip(playerId, drip, rounds);
    }
}
