using Content.Shared.Chemistry.Components;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class FluidPoolComponent : Component
{
    [ViewVariables]
    public HashSet<Vector2i> Tiles = new(1000);

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    [DataField("solution")] public string SolutionName = "pool";

}
