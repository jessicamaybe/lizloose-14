namespace Content.Shared._UM.Drip.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class TrackedDripComponent : Component
{
    /// <summary>
    /// How many rounds should be given for finding this drip
    /// </summary>
    [DataField]
    public int Rounds = 3;
}
