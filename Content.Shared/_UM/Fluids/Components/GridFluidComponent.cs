using System.Numerics;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class GridFluidComponent : Component
{
    [ViewVariables]
    public Dictionary<Vector2i, TileSolution> Tiles = new();

    [ViewVariables]
    public HashSet<Vector2i> ExcitedTiles = new(1000);

    [ViewVariables]
    public HashSet<Vector2i> FillStateChanged = new(1000);

    /// <summary>
    /// Tiles that will be deleted next update
    /// </summary>
    [ViewVariables]
    public HashSet<Vector2i> DeletedTiles = new(1000);

    [ViewVariables]
    public Dictionary<Vector2i, EntityUid> DrawnTiles = new();

    /// <summary>
    /// How manu units a puddle can hold before trying to spill over
    /// </summary>
    [ViewVariables]
    public FixedPoint2 OverflowVolume = 50;

    [ViewVariables]
    public List<FluidPool> Pools = new();

}



/// <summary>
/// The puddle has advanced into a pool
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class FluidPool
{


}
