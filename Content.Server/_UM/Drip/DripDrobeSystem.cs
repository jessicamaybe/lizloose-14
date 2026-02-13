using Content.Shared._UM.Drip.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._UM.Drip;

/// <summary>
/// This handles...
/// </summary>
public sealed class DripDrobeSystem : EntitySystem
{
    [Dependency] private readonly DripTrackingManager _dripTracking = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DripDrobeComponent, DripDrobeDispenseItemMessage>(OnRequestDispense);
    }

    private void OnRequestDispense(Entity<DripDrobeComponent> ent, ref DripDrobeDispenseItemMessage args)
    {
        if (!_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            return;

        var availableEntities = _dripTracking.GetAvailableEntities(session);

        if (!availableEntities.TryGetValue(args.Item.Id, out var amount) || amount <= 0)
            return;

        SpawnNextToOrDrop(args.Item.Id, ent.Owner);

        _dripTracking.SetDripRounds(session, args.Item.Id, amount - 1);
    }
}
