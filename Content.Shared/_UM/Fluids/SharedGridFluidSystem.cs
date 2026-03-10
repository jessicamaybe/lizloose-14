using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SharedGridFluidSystem : EntitySystem
{
    public const byte ChunkSize = 8;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

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


    public void AddFluid(Entity<TileFluidComponent?> ent, Solution solution)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Solution == null)
            return;

        ent.Comp.Solution.AddSolution(solution, _prototype);
        DirtyTile((ent, ent.Comp));
    }

    public virtual void DirtyTile(Entity<TileFluidComponent> ent)
    {
    }

    public bool TryGetSolution(Entity<TileFluidComponent?> ent, [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Solution == null)
            return false;

        solution = ent.Comp.Solution;
        Dirty(ent);
        return true;
    }

    public static Vector2i GetFluidChunkIndices(Vector2i indices)
    {
        return new((int) MathF.Floor((float) indices.X / ChunkSize), (int) MathF.Floor((float) indices.Y / ChunkSize));
    }


}
