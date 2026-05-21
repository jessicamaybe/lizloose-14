using Content.Shared._UM.Ghost;
using Content.Shared._UM.Ghost.Components;
using Content.Shared.Ghost;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._UM.Ghost;

/// <summary>
/// This handles...
/// </summary>
public sealed class UMGhostGroupInteractableSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostActionMenuRequestEvent>(OnGhostActionMenuRequest);
    }


    private void OnGhostActionMenuRequest(GhostActionMenuRequestEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ev.Actions == null)
            return;

        var ghostUi = _userInterfaceManager.GetUIController<UMGhostGroupUIController>();
        ghostUi.OpenActionPicker(ev.Entity, ev.Actions);
    }

    public void RequestVote(NetEntity target, GhostGroupAction action)
    {
        if ( _playerManager.LocalEntity is not {} user)
            return;

        //if (!HasComp<GhostComponent>(user))
        //    return;


    }
}
