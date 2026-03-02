using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

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
    public Dictionary<Vector2i, EntityUid> DrawnTiles = new();

    [ViewVariables]
    public EntityUid GridUid;

    [DataField]
    public FixedPoint2 OverflowVolume = FixedPoint2.New(50);

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    [DataField("solution")] public string SolutionName = "pool";

    [ViewVariables]
    public bool NeedsUpdate = false;
}
