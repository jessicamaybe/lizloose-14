using Robust.Shared.GameStates;

namespace Content.Shared._UM.Drip.Components;

/// <summary>
/// This is used for...
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
}
