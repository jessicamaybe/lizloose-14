using Robust.Shared.GameStates;

namespace Content.Shared._UM.Ghost.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UMGhostGroupInteractableComponent : Component
{
    //Amount of ghosts needed to trigger thing
    [DataField, AutoNetworkedField]
    public int Amount = 1;

    /// <summary>
    /// Ghosts currently voting to interact
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> CurrentGhosts = new();

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(120);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan LastActivated = new TimeSpan();
}


[ByRefEvent]
public record struct GhostGroupInteractEvent();
