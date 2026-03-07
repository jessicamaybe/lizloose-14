using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.EntityEffects;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChemicalReactionSystem _solutionReaction = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly GridFluidVisualsSystem _gridFluidVisuals = default!;

    private EntityQuery<AirtightComponent> _airtightQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializeSource();
        _airtightQuery = GetEntityQuery<AirtightComponent>();

        SubscribeLocalEvent<GridFluidComponent, GridSplitEvent>(OnGridSplit);
        SubscribeLocalEvent<GridFluidComponent, TileChangedEvent>(OnTileChange);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateFluidProcessing(frameTime);
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

    private void MoveTile(Entity<GridFluidComponent> oldGrid, Entity<GridFluidComponent> newGrid, TileSolution tile)
    {
        RemoveTile(oldGrid, tile);
        newGrid.Comp.Tiles.Add(tile.GridIndices, tile);
        InvalidateTile(newGrid, tile);
        _gridFluidVisuals.MoveTile(oldGrid, newGrid, tile);
    }

    private void AddTile(Entity<GridFluidComponent> ent, Vector2i indices, Solution solution, bool active = true)
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
    }

    private bool TryGetFluid(GridFluidComponent gridFluid,
        Vector2i indices,
        [NotNullWhen(true)] out TileSolution? tile)
    {
        tile = null;

        return gridFluid.Tiles.TryGetValue(indices, out tile);
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

    private bool AddActiveTile(GridFluidComponent gridFluid, TileSolution tile)
    {
        return gridFluid.ActiveTiles.Add(tile.GridIndices);
    }

    private bool AddActiveTile(GridFluidComponent gridFluid, Vector2i indices)
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

    private void AddTileReaction(GridFluidComponent gridFluid, TileSolution tile)
    {
        if (!gridFluid.Tiles.ContainsValue(tile))
            return;

        gridFluid.UnreactedTiles.Add(tile);
    }
}
