using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ChemicalReactionSystem _solutionReaction = default!;
    [Dependency] private readonly AtmosPipeColorSystem _pipeColor = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private EntityQuery<AirtightComponent> _airtightQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializeSource();
        _airtightQuery = GetEntityQuery<AirtightComponent>();

        SubscribeLocalEvent<GridFluidComponent, GridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(Entity<GridFluidComponent> ent, ref GridSplitEvent args)
    {

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


    private void AddTile(Entity<GridFluidComponent> ent, Vector2i indices, Solution solution, bool active = true)
    {
        var gridFluid = EnsureComp<GridFluidComponent>(ent.Owner);

        var tileSolution = new TileSolution(ent.Owner, indices);
        tileSolution.Excited = true;
        tileSolution.Solution.AddSolution(solution, _prototypeManager);
        gridFluid.Tiles.TryAdd(indices, tileSolution);
        InvalidateTile(gridFluid, tileSolution);
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

    private void AddActiveTile(GridFluidComponent gridFluid, TileSolution tile)
    {
        if (!gridFluid.Tiles.ContainsValue(tile))
            return;

        gridFluid.ActiveTiles.Add(tile.GridIndices);
    }

    private void AddActiveTile(GridFluidComponent gridFluid, Vector2i indices)
    {
        if (!gridFluid.Tiles.ContainsKey(indices))
            return;

        gridFluid.ActiveTiles.Add(indices);
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
