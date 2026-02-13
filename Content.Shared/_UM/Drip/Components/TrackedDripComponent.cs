using Robust.Shared.GameStates;

namespace Content.Shared._UM.Drip.Components;

/// <summary>
/// This is used for tracking items. If a player keeps an item with this component until the end of the round,
/// it'll be available in a "dripdrobe" machine
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TrackedDripComponent : Component
{
    /// <summary>
    /// How many rounds should be given for finding this drip
    /// </summary>
    [DataField]
    public int Rounds = 3;

    /// <summary>
    /// Is this drip spent? (Won't count towards tracking)
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool Spent = false;

    /// <summary>
    /// Whether or not the player has to be on the evac shuttle at round end for this to track
    /// </summary>
    [DataField]
    public bool RequireShuttle = false;
}
