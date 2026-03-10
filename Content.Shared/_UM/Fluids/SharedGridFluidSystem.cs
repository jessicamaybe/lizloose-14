using Content.Shared._UM.Fluids.Components;
using Content.Shared.Examine;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SharedGridFluidSystem : EntitySystem
{
    public const byte ChunkSize = 8;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    protected bool PvsEnabled;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializeExamine();
        SubscribeLocalEvent<GridFluidComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(Entity<GridFluidComponent> ent, ref ComponentGetState args)
    {
        if (PvsEnabled && !args.ReplayState)
            return;

        if (args.FromTick <= ent.Comp.CreationTick)
        {
            args.State = new GridFluidState(ent.Comp.Chunks);
            return;
        }

        var data = new Dictionary<Vector2i, FluidChunk>();
        foreach (var (index, chunk) in ent.Comp.Chunks)
        {
            if (chunk.LastModified >= args.FromTick)
                data[index] = chunk;
        }
        args.State = new GridFluidDeltaState(data, new(ent.Comp.Chunks.Keys));
    }




    public virtual void DirtyTile(Entity<TileFluidComponent> ent)
    {
    }



    public static Vector2i GetFluidChunkIndices(Vector2i indices)
    {
        return new((int) MathF.Floor((float) indices.X / ChunkSize), (int) MathF.Floor((float) indices.Y / ChunkSize));
    }


}
