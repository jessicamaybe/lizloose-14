using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chunking;
using Content.Shared.EntityEffects;
using Content.Shared.Maps;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem : SharedGridFluidSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChemicalReactionSystem _solutionReaction = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GridFluidVisualsSystem _gridFluidVisuals = default!;
    [Dependency] private readonly IConfigurationManager _conf = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IParallelManager _parMan = default!;
    [Dependency] private readonly ChunkingSystem _chunkingSys = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    private readonly Dictionary<ICommonSession, Dictionary<NetEntity, HashSet<Vector2i>>> _lastSentChunks = new();

    private ObjectPool<HashSet<Vector2i>> _chunkIndexPool =
        new DefaultObjectPool<HashSet<Vector2i>>(
            new DefaultPooledObjectPolicy<HashSet<Vector2i>>(), 64);
    private ObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>> _chunkViewerPool =
        new DefaultObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>>(
            new DefaultPooledObjectPolicy<Dictionary<NetEntity, HashSet<Vector2i>>>(), 64);


    private readonly List<ICommonSession> _sessions = new();

    private UpdatePlayerJob _updateJob;
    private bool _doSessionUpdate;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializeSource();
        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();

        _updateJob = new UpdatePlayerJob()
        {
            EntManager = EntityManager,
            System = this,
            ChunkIndexPool = _chunkIndexPool,
            Sessions = _sessions,
            ChunkingSys = _chunkingSys,
            MapManager = _mapManager,
            ChunkViewerPool = _chunkViewerPool,
            LastSentChunks = _lastSentChunks,
            GridQuery = _gridQuery,
        };

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeLocalEvent<GridFluidComponent, GridSplitEvent>(OnGridSplit);
        SubscribeLocalEvent<GridFluidComponent, TileChangedEvent>(OnTileChange);

        Subs.CVar(_conf, CVars.NetPVS, OnPvsToggle, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_doSessionUpdate)
        {
            UpdateSessions();
            return;
        }

        UpdateFluidProcessing(frameTime);
        _doSessionUpdate = true;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.InGame)
        {
            if (_lastSentChunks.Remove(e.Session, out var sets))
            {
                foreach (var set in sets.Values)
                {
                    set.Clear();
                    _chunkIndexPool.Return(set);
                }
            }
        }

        if (!_lastSentChunks.ContainsKey(e.Session))
        {
            _lastSentChunks[e.Session] = new();
        }
    }

    private void OnPvsToggle(bool value)
    {
        if (value == PvsEnabled)
            return;

        PvsEnabled = value;

        if (value)
            return;

        foreach (var playerData in _lastSentChunks.Values)
        {
            playerData.Clear();
        }

        var query = AllEntityQuery<GridFluidComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var grid, out var meta))
        {
            grid.ForceTick = _timing.CurTick;
            Dirty(uid, grid, meta);
        }
    }

    private void UpdateSessions()
    {
        _doSessionUpdate = false;

        if (!PvsEnabled)
            return;

        _sessions.Clear();

        foreach (var player in _playerManager.Sessions)
        {
            if (player.Status != SessionStatus.InGame)
                continue;

            _sessions.Add(player);
        }

        if (_sessions.Count == 0)
            return;

        _parMan.ProcessNow(_updateJob, _sessions.Count);
        _updateJob.LastSessionUpdate = _timing.CurTick;
    }

    private void OnTileChange(Entity<GridFluidComponent> ent, ref TileChangedEvent args)
    {
        foreach (var change in args.Changes)
        {
            if (change.EmptyChanged)
                continue;

            if (change.NewTile.IsEmpty)
                continue;

            if (TryGetFluid(ent.Comp, change.GridIndices, out var tile))
            {
                RemoveTile(ent, tile);
            }
        }
    }

    private void UpdateBlockedDirections(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent,
        TileSolution tile,
        bool activate = false)
    {
        var gridFluid = ent.Comp1;

        for (var i = 0; i < 4; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            var neighborPos = tile.GridIndices.Offset(direction);

            if (IsTileBlocked((ent.Owner, ent.Comp2, ent.Comp3), neighborPos))
            {
                tile.BlockedDirections &= ~direction;
            }
            else
            {
                tile.BlockedDirections |= direction;
            }
        }

        if (activate)
            AddActiveTile(ent.Comp1, tile);

        gridFluid.InvalidTiles.Remove(tile);
    }

    private void OnGridSplit(Entity<GridFluidComponent> ent, ref GridSplitEvent args)
    {
        foreach (var newGrid in args.NewGrids)
        {
            if (!TryComp<MapGridComponent>(newGrid, out var gridComp))
                return;

            var gridFluid = EnsureComp<GridFluidComponent>(newGrid);

            foreach (var tile in _map.GetAllTiles(newGrid, gridComp))
            {
                if (TryGetFluid(ent.Comp, tile.GridIndices, out var tileSolution))
                {
                    MoveTile(ent, (newGrid, gridFluid), tileSolution);
                }
            }
        }
    }

    /// <summary>
    /// Move tile to new grid on grid spl
    /// </summary>
    /// <param name="oldGrid"></param>
    /// <param name="newGrid"></param>
    /// <param name="tile"></param>
    private void MoveTile(Entity<GridFluidComponent> oldGrid, Entity<GridFluidComponent> newGrid, TileSolution tile)
    {
        RemoveTile(oldGrid, tile);
        newGrid.Comp.Tiles.Add(tile.GridIndices, tile);
        InvalidateTile(newGrid, tile);
        _gridFluidVisuals.MoveTile(oldGrid, newGrid, tile);
    }

    public override void AddTile(Entity<GridFluidComponent> ent, Vector2i indices, Solution solution, bool active = true)
    {
        var gridFluid = EnsureComp<GridFluidComponent>(ent.Owner);

        var tileSolution = new TileSolution(ent.Owner, indices);
        tileSolution.Excited = true;
        tileSolution.Solution.AddSolution(solution, _prototypeManager);
        gridFluid.Tiles.TryAdd(indices, tileSolution);
        InvalidateTile(gridFluid, tileSolution);
        _gridFluidVisuals.MarkInvalid(ent, indices);
    }

    private void RemoveTile(GridFluidComponent gridFluid, TileSolution tile)
    {
        gridFluid.Tiles.Remove(tile.GridIndices);
        gridFluid.ActiveTiles.Remove(tile.GridIndices);
        gridFluid.InvalidTiles.Remove(tile);
        gridFluid.UnreactedTiles.Remove(tile);
        MarkModifiedTile(gridFluid, tile.GridIndices);
    }

    private void InvalidateTile(GridFluidComponent gridFluid, TileSolution tile)
    {
        gridFluid.InvalidTiles.Add(tile);
    }

    private void InvalidateTile(GridFluidComponent gridFluid, Vector2i indices)
    {
        if (!gridFluid.Tiles.TryGetValue(indices, out var tile))
        {
            for (var i = 0; i < 4; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var neighborPos = indices.Offset(direction);
                if (TryGetFluid(gridFluid, neighborPos, out var neighbor))
                    InvalidateTile(gridFluid, neighbor);
            }

            return;
        }

        gridFluid.InvalidTiles.Add(tile);
    }

    public override bool AddActiveTile(GridFluidComponent gridFluid, TileSolution tile)
    {
        return gridFluid.ActiveTiles.Add(tile.GridIndices);
    }

    public override bool AddActiveTile(GridFluidComponent gridFluid, Vector2i indices)
    {
        return gridFluid.ActiveTiles.Add(indices);
    }

    private void RemoveActiveTile(Entity<MapGridComponent, GridFluidComponent> ent, Vector2i indices)
    {
        if (!ent.Comp2.Tiles.ContainsKey(indices) || !ent.Comp2.ActiveTiles.Contains(indices))
            return;

        ent.Comp2.ActiveTiles.Remove(indices);
    }

    private void RemoveActiveTile(GridFluidComponent gridFluid, Vector2i indices)
    {
        if (!gridFluid.Tiles.ContainsKey(indices) || !gridFluid.ActiveTiles.Contains(indices))
            return;

        gridFluid.ActiveTiles.Remove(indices);
    }

    public override void AddTileReaction(GridFluidComponent gridFluid, TileSolution tile)
    {
        if (!gridFluid.Tiles.ContainsValue(tile))
            return;

        gridFluid.UnreactedTiles.Add(tile);
    }

    private bool UpdateChunkTile(GridFluidComponent gridFluid, FluidChunk chunk, Vector2i index)
    {
        if (!gridFluid.Tiles.TryGetValue(index, out var tile))
        {
            chunk.Tiles.Remove(index);
            chunk.LastModified = _timing.CurTick;
            return true;
        }

        if (!chunk.Tiles.TryGetValue(index, out var chunkTile))
        {
            chunk.Tiles.TryAdd(index, new TileSolution(tile));
            chunk.LastModified = _timing.CurTick;
            return true;
        }

        if (chunkTile.Solution.Contents == tile.Solution.Contents)
        {
            chunk.LastModified = _timing.CurTick;
            return false;
        }

        chunkTile.Solution = tile.Solution.Clone();
        chunk.LastModified = _timing.CurTick;
        return true;
    }

    private void UpdateFluidData(Entity<GridFluidComponent, MapGridComponent, TransformComponent> ent)
    {
        var gridFluid = ent.Comp1;

        var changed = false;
        foreach (var index in gridFluid.ModifiedTiles)
        {
            var chunkIndex = GetFluidChunkIndices(index);

            if (!gridFluid.Chunks.TryGetValue(chunkIndex, out var chunk))
            {
                gridFluid.Chunks[chunkIndex] = chunk = new FluidChunk(chunkIndex);
            }

            changed |= UpdateChunkTile(gridFluid, chunk, index);
        }

        if (changed)
            Dirty(ent.Owner, gridFluid);

        gridFluid.ModifiedTiles.Clear();
    }

    private record struct UpdatePlayerJob : IParallelRobustJob
    {
        public int BatchSize => 2;

        public IEntityManager EntManager;
        public IMapManager MapManager;
        public ChunkingSystem ChunkingSys;
        public GridFluidSystem System;
        public ObjectPool<HashSet<Vector2i>> ChunkIndexPool;
        public ObjectPool<Dictionary<NetEntity, HashSet<Vector2i>>> ChunkViewerPool;

        public GameTick LastSessionUpdate;
        public Dictionary<ICommonSession, Dictionary<NetEntity, HashSet<Vector2i>>> LastSentChunks;
        public List<ICommonSession> Sessions;

        public EntityQuery<MapGridComponent> GridQuery;

        public void Execute(int index)
        {
            var playerSession = Sessions[index];
            var chunksInRange =
                ChunkingSys.GetChunksForSession(playerSession, ChunkSize, ChunkIndexPool, ChunkViewerPool);
            var previouslySent = LastSentChunks[playerSession];

            var ev = new FluidChunkUpdateEvent();

            foreach (var (netGrid, oldIndices) in previouslySent)
            {
                if (!chunksInRange.TryGetValue(netGrid, out var chunks))
                {
                    previouslySent.Remove(netGrid);

                    if (!EntManager.TryGetEntity(netGrid, out var gridId) || GridQuery.HasComp(gridId.Value))
                        ev.RemovedChunks[netGrid] = oldIndices;
                    else
                    {
                        oldIndices.Clear();
                        ChunkIndexPool.Return(oldIndices);
                    }
                    continue;
                }
                var old = ChunkIndexPool.Get();
                    DebugTools.Assert(old.Count == 0);
                    foreach (var chunk in oldIndices)
                    {
                        if (!chunks.Contains(chunk))
                            old.Add(chunk);
                    }

                    if (old.Count == 0)
                        ChunkIndexPool.Return(old);
                    else
                        ev.RemovedChunks.Add(netGrid, old);
            }

            foreach (var (netGrid, gridChunks) in chunksInRange)
            {
                // Not all grids have fluids
                if (!EntManager.TryGetEntity(netGrid, out var grid) ||
                    !EntManager.TryGetComponent(grid, out GridFluidComponent? gridFluid))
                    continue;

                List<FluidChunk> dataToSend = new();
                ev.UpdatedChunks[netGrid] = dataToSend;

                previouslySent.TryGetValue(netGrid, out var previousChunks);

                foreach (var gIndex in gridChunks)
                {
                    if (!gridFluid.Chunks.TryGetValue(gIndex, out var value))
                        continue;

                    // If the chunk was updated since we last sent it, send it again
                    if (value.LastModified > LastSessionUpdate)
                    {
                        dataToSend.Add(value);
                        continue;
                    }

                    // Always send it if we didn't previously send it
                    if (previousChunks == null || !previousChunks.Contains(gIndex))
                        dataToSend.Add(value);
                }

                previouslySent[netGrid] = gridChunks;
                if (previousChunks != null)
                {
                    previousChunks.Clear();
                    ChunkIndexPool.Return(previousChunks);
                }
            }

            if (ev.UpdatedChunks.Count != 0 || ev.RemovedChunks.Count != 0)
                    System.RaiseNetworkEvent(ev, playerSession.Channel);
        }
    }
}
