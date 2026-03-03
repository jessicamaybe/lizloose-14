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
    public HashSet<Entity<FluidPoolComponent>> AddedTiles = new();

    /// <summary>
    /// List of pools that will be deleted next update cycle
    /// </summary>
    [ViewVariables]
    public HashSet<Entity<FluidPoolComponent>> DeletedTiles = new();

    /// <summary>
    /// List of pools that need to be updated
    /// </summary>
    public HashSet<Entity<FluidPoolComponent>> StalePools = new();



    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;


    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(100);


    [ViewVariables]
    public readonly Queue<Entity<FluidPoolComponent>> CurrentRunPools = new();
}
