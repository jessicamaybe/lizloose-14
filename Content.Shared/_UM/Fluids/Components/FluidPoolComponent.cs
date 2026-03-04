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
    /// <summary>
    /// List of tiles that this pool occupies
    /// </summary>
    [ViewVariables]
    public HashSet<TileRef> Tiles = new(1000);

    /// <summary>
    /// List of tiles which are considered to be the "edge"
    /// </summary>
    [ViewVariables]
    public HashSet<TileRef> EdgeTiles = new(1000);

    /// <summary>
    /// Tiles we're adding on the next update
    /// </summary>
    public HashSet<TileRef> AddedTiles = new();

    /// <summary>
    /// Tiles we're removing on the next update
    /// </summary>
    public HashSet<TileRef> RemovedTiles = new();

    /// <summary>
    /// Temporary
    /// The entities representing this puddle
    /// </summary>
    [ViewVariables]
    public Dictionary<TileRef, EntityUid> DrawnTiles = new();

    /// <summary>
    /// The grid entity this pool is on
    /// </summary>
    [ViewVariables]
    public EntityUid GridUid;

    /// <summary>
    /// The volume at which this puddle should overflow
    /// </summary>
    [DataField]
    public FixedPoint2 OverflowVolume = FixedPoint2.New(50);

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    [DataField("solution")]
    public string SolutionName = "pool";

    /// <summary>
    /// How overflowed this puddle is.
    /// </summary>
    [ViewVariables]
    public PoolFillLevel FillLevel = PoolFillLevel.Puddle;

    /// <summary>
    /// How filled this puddle was
    /// </summary>
    [ViewVariables]
    public PoolFillLevel FillLevelLastRun = PoolFillLevel.Puddle;

    /// <summary>
    /// Was the room full last time we tried to update?
    /// </summary>
    [ViewVariables]
    public bool RoomFull = false;
}

/// <summary>
/// Puddle height levels
/// </summary>
public enum PoolFillLevel : byte
{
    Puddle,
    AnkleHeight,
    WaistHeight,
    Full,
}
