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

    /// <summary>
    /// Currently active tiles
    /// </summary>
    [ViewVariables]
    public HashSet<Vector2i> ActiveTiles = new(1000);

    [ViewVariables]
    public readonly Queue<Vector2i> CurrentRunTiles = new();

    /// <summary>
    /// Tiles that need to be revalidated
    /// </summary>
    [ViewVariables]
    public HashSet<TileSolution> InvalidTiles = new();

    [ViewVariables]
    public readonly Queue<TileSolution> CurrentRunInvalidTiles = new();

    /// <summary>
    /// Tiles which still need to be checked for reactions
    /// </summary>
    [ViewVariables]
    public HashSet<TileSolution> UnreactedTiles = new();

    [ViewVariables]
    public readonly Queue<TileSolution> CurrentRunUnreactedTiles = new();


    //Tile group stuff
    [ViewVariables]
    public List<TileSolutionGroup> TileGroups = new();

    [ViewVariables]
    public readonly Queue<TileSolutionGroup> CurrentRunTileGroups = new();


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
