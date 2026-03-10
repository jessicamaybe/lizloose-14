using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using static Content.Shared._UM.Fluids.SharedGridFluidSystem;


namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, NetworkedComponent]
public sealed partial class GridFluidComponent : Component
{
    [ViewVariables]
    public Dictionary<Vector2i, TileSolution> Tiles = new();

    [ViewVariables]
    public readonly Dictionary<Vector2i, FluidChunk> Chunks = new();

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

    /// <summary>
    /// Tiles that have had their data changed in the last tick
    /// </summary>
    [ViewVariables]
    public readonly HashSet<Vector2i> ModifiedTiles = new();


    public readonly Dictionary<Vector2i, EntityUid> DrawnTiles = new();

    /// <summary>
    /// How manu units a puddle can hold before trying to spill over
    /// </summary>
    [ViewVariables]
    public FixedPoint2 OverflowVolume = 50;

    public int Stage = 1;


    /// <summary>
    /// The next time we remove the EvaporationSystem reagent amount from this entity.
    /// </summary>
    [AutoPausedField]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    /// <summary>
    /// Evaporation factor. Multiplied by the evaporating speed of the reagent.
    /// </summary>
    [DataField]
    public FixedPoint2 EvaporationAmount = FixedPoint2.New(1);

    /// <summary>
    ///     Tick at which PVS was last toggled. Ensures that all players receive a full update when toggling PVS.
    /// </summary>
    public GameTick ForceTick { get; set; }

}


[Serializable, NetSerializable]
[Access(typeof(SharedGridFluidSystem))]
public sealed class FluidChunk
{
    public readonly Vector2i Index;
    public readonly Vector2i Origin;

    [ViewVariables]
    public Dictionary<Vector2i, TileSolution> Tiles = new();

    [NonSerialized]
    public GameTick LastModified;

    public FluidChunk(Dictionary<Vector2i, TileSolution> tiles)
    {
        Tiles = tiles;
    }

    public FluidChunk(Vector2i index)
    {
        Index = index;
        Origin = Index * ChunkSize;
    }

}

[Serializable, NetSerializable]
public sealed class GridFluidState(Dictionary<Vector2i, FluidChunk> chunks) : ComponentState
{
    public Dictionary<Vector2i, FluidChunk> Chunks = chunks;
}


[Serializable, NetSerializable]
public sealed class GridFluidDeltaState(Dictionary<Vector2i, FluidChunk> modifiedChunks, HashSet<Vector2i> allChunks)
    : ComponentState, IComponentDeltaState<GridFluidState>
{
    public Dictionary<Vector2i, FluidChunk> ModifiedChunks = modifiedChunks;
    public HashSet<Vector2i> AllChunks = allChunks;

    public void ApplyToFullState(GridFluidState state)
    {
        foreach (var key in state.Chunks.Keys)
        {
            if (!AllChunks!.Contains(key))
                state.Chunks.Remove(key);
        }

        foreach (var (chunk, data) in ModifiedChunks)
        {
            state.Chunks[chunk] = data;
        }
    }

    public GridFluidState CreateNewFullState(GridFluidState state)
    {
        var chunks = new Dictionary<Vector2i, FluidChunk>(state.Chunks.Count);

        foreach (var (chunk, data) in ModifiedChunks)
        {
            chunks[chunk] = data;
        }

        foreach (var (chunk, data) in state.Chunks)
        {
            if (AllChunks!.Contains(chunk))
                chunks.TryAdd(chunk, data);
        }
        return new GridFluidState(chunks);
    }
}


[Serializable, NetSerializable]
public sealed class FluidChunkUpdateEvent : EntityEventArgs
{
    public Dictionary<NetEntity, List<FluidChunk>> UpdatedChunks = new();
    public Dictionary<NetEntity, HashSet<Vector2i>> RemovedChunks = new();
}
