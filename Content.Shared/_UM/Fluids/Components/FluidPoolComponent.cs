using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class FluidPoolComponent : Component
{
    [ViewVariables]
    public HashSet<TileRef> Tiles = new(1000);

    [ViewVariables]
    public Dictionary<TileRef, EntityUid> DrawnTiles = new();

    [ViewVariables]
    public EntityUid GridUid;

    [DataField]
    public FixedPoint2 OverflowVolume = FixedPoint2.New(50);

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    [DataField("solution")]
    public string SolutionName = "pool";

    /// <summary>
    /// How overflowed this puddle is.
    /// 0 is normal puddle
    /// 1 is mild overflowing
    ///
    /// </summary>
    [ViewVariables]
    public int OverFlowLevel = 0;

    [ViewVariables]
    public bool LastUpdateStuck = false;

    [ViewVariables]
    public bool NeedsUpdate = false;
}
