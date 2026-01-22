using Content.Shared.DeviceLinking.Events;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Content.Shared.Power;
using Content.Shared.Prying.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Wires;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

public abstract class SharedAirlockSystem : EntitySystem
{
    [Dependency] private   readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private   readonly SharedWiresSystem _wiresSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<AirlockComponent, DoorBoltsChangedEvent>(OnBoltsChanged);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<AirlockComponent, GetPryTimeModifierEvent>(OnGetPryMod);
        SubscribeLocalEvent<AirlockComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<AirlockComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnActivate, before: new[] { typeof(SharedDoorSystem) });
    }

    private void OnBeforeDoorClosed(Entity<AirlockComponent> ent, ref BeforeDoorClosedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Safety)
            args.PerformCollisionCheck = false;

        // only block based on bolts / power status when initially closing the door, not when its already
        // mid-transition. Particularly relevant for when the door was pried-closed with a crowbar, which bypasses
        // the initial power-check.

        if (HasComp<DoorComponent>(ent)
            && !args.Partial
            && !CanChangeState(ent))
        {
            args.Cancel();
        }
    }

    private void OnStateChanged(Entity<AirlockComponent> ent, ref DoorStateChangedEvent args)
    {
        // This is here so we don't accidentally bulldoze state values and mispredict.
        if (_timing.ApplyingState)
            return;

        // Only show the maintenance panel if the airlock is closed
        if (TryComp<WiresPanelComponent>(ent, out var wiresPanel))
        {
            _wiresSystem.ChangePanelVisibility(ent, wiresPanel, ent.Comp.OpenPanelVisible || args.State != DoorState.Open);
        }
        // If the door is closed, we should look if the bolt was locked while closing
        UpdateAutoClose(ent);

        // Make sure the airlock auto closes again next time it is opened
        if (args.State == DoorState.Closed)
        {
            ent.Comp.AutoClose = true;
            Dirty(ent);
        }
    }

    private void OnBoltsChanged(Entity<AirlockComponent> ent, ref DoorBoltsChangedEvent args)
    {
        // If unbolted, reset the auto close timer
        if (!args.BoltsDown)
            UpdateAutoClose(ent);
    }

    private void OnBeforeDoorOpened(Entity<AirlockComponent> ent, ref BeforeDoorOpenedEvent args)
    {
        if (!CanChangeState(ent))
            args.Cancel();
    }

    private void OnBeforeDoorDenied(Entity<AirlockComponent> ent, ref BeforeDoorDeniedEvent args)
    {
        if (!CanChangeState(ent))
            args.Cancel();
    }

    private void OnGetPryMod(Entity<AirlockComponent> ent, ref GetPryTimeModifierEvent args)
    {
        if (ent.Comp.Powered)
            args.PryTimeModifier *= ent.Comp.PoweredPryModifier;

        if (_doorSystem.IsBolted(ent.Owner))
            args.PryTimeModifier *= ent.Comp.BoltedPryModifier;
    }

    /// <summary>
    /// Updates the auto close timer.
    /// </summary>
    public void UpdateAutoClose(Entity<AirlockComponent> ent, DoorComponent? door = null)
    {
        if (!Resolve(ent.Owner, ref door, ref door))
            return;

        if (door.State != DoorState.Open)
            return;

        if (!ent.Comp.AutoClose)
            return;

        if (!CanChangeState((ent.Owner, ent.Comp)))
            return;

        var autoev = new BeforeDoorAutoCloseEvent();
        RaiseLocalEvent(ent, autoev);
        if (autoev.Cancelled)
            return;

        _doorSystem.SetNextStateChange(ent.Owner, ent.Comp.AutoCloseDelay * ent.Comp.AutoCloseDelayModifier);
    }

    private void OnBeforePry(Entity<AirlockComponent> ent, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Powered || args.PryPowered)
            return;

        args.Message = "airlock-component-cannot-pry-is-powered-message";

        args.Cancelled = true;
    }

    public void UpdateEmergencyLightStatus(Entity<AirlockComponent> ent)
    {
        _appearance.SetData(ent, DoorVisuals.EmergencyLights, ent.Comp.EmergencyAccess);
    }

    public void SetEmergencyAccess(Entity<AirlockComponent> ent, bool value, EntityUid? user = null, bool predicted = false)
    {
        if(!ent.Comp.Powered)
            return;

        if (ent.Comp.EmergencyAccess == value)
            return;

        ent.Comp.EmergencyAccess = value;
        Dirty(ent, ent.Comp); // This only runs on the server apparently so we need this.
        UpdateEmergencyLightStatus(ent);

        var sound = ent.Comp.EmergencyAccess ? ent.Comp.EmergencyOnSound : ent.Comp.EmergencyOffSound;
        if (predicted)
            _audio.PlayPredicted(sound, ent, user: user);
        else
            _audio.PlayPvs(sound, ent);
    }

    public void SetAutoCloseDelayModifier(AirlockComponent component, float value)
    {
        if (component.AutoCloseDelayModifier.Equals(value))
            return;

        component.AutoCloseDelayModifier = value;
    }

    public void SetSafety(AirlockComponent component, bool value)
    {
        component.Safety = value;
    }

    public bool CanChangeState(Entity<AirlockComponent> ent)
    {
        return ent.Comp.Powered && !_doorSystem.IsBolted(ent.Owner);
    }

    private void OnSignalReceived(Entity<AirlockComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.AutoClosePort && ent.Comp.AutoClose)
        {
            ent.Comp.AutoClose = false;
            Dirty(ent);
        }
    }

    private void OnPowerChanged(Entity<AirlockComponent> ent, ref PowerChangedEvent args)
    {
        ent.Comp.Powered = args.Powered;
        Dirty(ent);

        if (!TryComp(ent, out DoorComponent? door))
            return;

        if (!args.Powered)
        {
            // stop any scheduled auto-closing
            if (door.State == DoorState.Open)
                _doorSystem.SetNextStateChange(ent, null);
        }
        else
        {
            UpdateAutoClose(ent, door: door);
        }
    }

    private void OnActivate(Entity<AirlockComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (TryComp<WiresPanelComponent>(ent, out var panel) &&
            panel.Open &&
            TryComp<ActorComponent>(args.User, out var actor) &&
            !HasComp<StationAiHeldComponent>(args.User))
        {
            if (TryComp<WiresPanelSecurityComponent>(ent, out var wiresPanelSecurity) &&
                !wiresPanelSecurity.WiresAccessible)
                return;

            _uiSystem.OpenUi(ent.Owner, WiresUiKey.Key, actor.PlayerSession);

            args.Handled = true;
            return;
        }

        if (ent.Comp.KeepOpenIfClicked && ent.Comp.AutoClose)
        {
            // Disable auto close
            ent.Comp.AutoClose = false;
            Dirty(ent);
        }
    }
}
