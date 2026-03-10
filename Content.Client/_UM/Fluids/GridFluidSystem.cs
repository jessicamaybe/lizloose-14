using System.Numerics;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;

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
                gridFluid.Chunks.Remove(index);
            }
            UpdateTiles((grid, gridFluid));
        }
        foreach (var (nent, gridData) in ev.UpdatedChunks)
        {
            var grid = GetEntity(nent);

            if (!TryComp<GridFluidComponent>(grid, out var gridFluid))
                continue;

            foreach (var chunkdata in gridData)
            {
                gridFluid.Chunks[chunkdata.Index] = chunkdata;
            }
            UpdateTiles((grid, gridFluid));
        }
    }

    private void UpdateTiles(Entity<GridFluidComponent> ent)
    {
        ent.Comp.Tiles.Clear();

        foreach (var (_, chunk) in ent.Comp.Chunks)
        {
            foreach (var tile in chunk.Tiles)
            {
                tile.Value.GridIndex = ent.Owner;
                ent.Comp.Tiles.Add(tile.Key, tile.Value);
            }
        }

        DrawTiles(ent);
    }


    /// <summary>
    /// Remove this, this was just here so i could visualize shit easy real quick ok
    /// </summary>
    /// <param name="ent"></param>
    private void DrawTiles(Entity<GridFluidComponent> ent)
    {
        if (!TryComp<MapGridComponent>(ent.Owner, out var mapgrid))
            return;

        foreach (var (indices, tile) in ent.Comp.Tiles)
        {
            if (ent.Comp.DrawnTiles.ContainsKey(indices))
                continue;

            var coords = _map.GridTileToLocal(ent.Owner, mapgrid, indices);
            var entity = Spawn("FluidPuddle", coords);
            ent.Comp.DrawnTiles.Add(indices, entity);
        }

        var deleted = new HashSet<Vector2i>();

        foreach (var (indices, _) in ent.Comp.DrawnTiles)
        {
            if (!ent.Comp.Tiles.ContainsKey(indices))
                deleted.Add(indices);
        }

        foreach (var todelete in deleted)
        {
            QueueDel(ent.Comp.DrawnTiles[todelete]);
            ent.Comp.DrawnTiles.Remove(todelete);
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
                Log.Debug("Getting GridFluidDeltaState chunk count: " + delta.ModifiedChunks.Count);
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
                Log.Debug("Getting gridfluidstate chunk count: " + state.Chunks.Count);
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
