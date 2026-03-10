using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Robust.Shared.GameStates;

namespace Content.Client._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed class GridFluidSystem : SharedGridFluidSystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<FluidChunkUpdateEvent>(HandleFluidUpdate);
        SubscribeLocalEvent<GridFluidComponent, ComponentHandleState>(OnHandleState);
    }

    private void HandleFluidUpdate(FluidChunkUpdateEvent ev)
    {
        foreach (var (nent, removedIndices) in ev.RemovedChunks)
        {
            var grid = GetEntity(nent);

            if (!TryComp<GridFluidComponent>(grid, out var gridFluid))
                continue;

            foreach (var index in removedIndices)
            {
                if (gridFluid.Chunks.TryGetValue(index, out var chunk))
                {
                    foreach (var (indices, _) in chunk.Tiles)
                    {
                        gridFluid.Tiles.Remove(indices);
                    }
                }
                gridFluid.Chunks.Remove(index);
            }
        }
        foreach (var (nent, gridData) in ev.UpdatedChunks)
        {
            var grid = GetEntity(nent);

            if (!TryComp<GridFluidComponent>(grid, out var gridFluid))
                continue;

            foreach (var chunkdata in gridData)
            {
                gridFluid.Chunks[chunkdata.Index] = chunkdata;

                foreach (var (indices, data) in chunkdata.Tiles)
                {
                    gridFluid.Tiles[indices] = data;
                    gridFluid.ModifiedTiles.Add(indices);
                }
            }
        }
    }

    private void OnHandleState(Entity<GridFluidComponent> ent, ref ComponentHandleState args)
    {
        Dictionary<Vector2i, FluidChunk> modifiedChunks;
        switch (args.Current)
        {
            case GridFluidDeltaState delta:
            {
                modifiedChunks = delta.ModifiedChunks;
                foreach (var index in ent.Comp.Chunks.Keys)
                {
                    if (!delta.AllChunks.Contains(index))
                        ent.Comp.Chunks.Remove(index);
                }

                break;
            }
            case GridFluidState state:
            {
                modifiedChunks = state.Chunks;
                foreach (var index in ent.Comp.Chunks.Keys)
                {
                    if (!state.Chunks.ContainsKey(index))
                        ent.Comp.Chunks.Remove(index);
                }
                break;
            }
            default:
                return;
        }

        foreach (var (index, data) in modifiedChunks)
        {
            ent.Comp.Chunks[index] = data;
        }
    }
}
