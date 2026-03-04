using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class GridFluidComponent : Component
{
    [ViewVariables]
    public List<Entity<FluidPoolComponent>> Pools = new();

    /// <summary>
    /// List of pools that will be added next update cycle
    /// </summary>
    [ViewVariables]
    public HashSet<Entity<FluidPoolComponent>> AddedPools = new();


    /// <summary>
    /// List of pools that will be deleted next update cycle
    /// </summary>
    [ViewVariables]
    public HashSet<Entity<FluidPoolComponent>> DeletedPools = new();

    /// <summary>
    /// List of pools that need to be updated
    /// </summary>
    public HashSet<Entity<FluidPoolComponent>> StalePools = new();

    [ViewVariables]
    public Dictionary<TileRef, PuddleMerger> Mergers = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;


    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Pools we're iterating on in our current run
    /// </summary>
    [ViewVariables]
    public readonly Queue<Entity<FluidPoolComponent>> CurrentRunPools = new();
}


[Serializable]
public sealed class PuddleMerger
{
    /// <summary>
    /// Indices of the merger, where the temporary solution containers should be (so explosions and shit work)
    /// </summary>
    public Vector2i Indices;

    /// <summary>
    /// Difference in volume between the two puddles at the starting point
    /// </summary>
    public FixedPoint2 Difference;

    /// <summary>
    /// How many steps this puddle merger will take
    /// </summary>
    public int Steps;

    public Entity<FluidPoolComponent> PoolA;

    public Entity<FluidPoolComponent> PoolB;


    public PuddleMerger(Vector2i indices, FixedPoint2 difference, int steps, Entity<FluidPoolComponent> poolA, Entity<FluidPoolComponent> poolB)
    {
        Indices = indices;
        Difference = difference;
        Steps = steps;
        PoolA = poolA;
        PoolB = poolB;
    }
}
