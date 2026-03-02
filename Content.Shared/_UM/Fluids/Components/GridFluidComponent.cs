namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class GridFluidComponent : Component
{
    [ViewVariables]
    public Dictionary<EntityUid, List<Vector2i>> Pools;
}
