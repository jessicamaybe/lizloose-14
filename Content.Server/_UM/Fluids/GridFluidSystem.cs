using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
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


    public void AddFluid(Entity<MapGridComponent> ent, EntityCoordinates coords, Solution solution)
    {
        if (!_map.TryGetTileRef(ent.Owner, ent.Comp, coords, out var tileRef))
            return;

        AddFluid(ent, tileRef.GridIndices, solution);
    }

    public void AddFluid(Entity<MapGridComponent> ent, Vector2i indices, Solution solution, bool active = true)
    {
        var gridFluid = EnsureComp<GridFluidComponent>(ent.Owner);

        if (gridFluid.Tiles.TryGetValue(indices, out var tile))
        {
            tile.Solution.AddSolution(solution, _prototypeManager);
            AddActiveTile((ent.Owner, ent.Comp, gridFluid), indices);
            return;
        }

        AddTile((ent.Owner, ent.Comp, gridFluid), indices, solution, active);
    }

    private void AddTile(Entity<MapGridComponent, GridFluidComponent> ent, Vector2i indices, Solution solution, bool active = true)
    {
        var gridFluid = EnsureComp<GridFluidComponent>(ent.Owner);

        var tileSolution = new TileSolution(ent.Owner, indices);
        tileSolution.Excited = true;
        tileSolution.Solution.AddSolution(solution, _prototypeManager);
        gridFluid.Tiles.TryAdd(indices, tileSolution);

        if (active)
            AddActiveTile(ent, indices);
    }


    private bool TryGetFluid(Entity<MapGridComponent, GridFluidComponent> ent,
        Vector2i indices,
        [NotNullWhen(true)] out TileSolution? tile)
    {
        tile = null;

        return ent.Comp2.Tiles.TryGetValue(indices, out tile);
    }

    private void AddActiveTile(Entity<MapGridComponent, GridFluidComponent> ent, Vector2i indices)
    {
        if (!ent.Comp2.Tiles.ContainsKey(indices))
            return;

        ent.Comp2.ActiveTiles.Add(indices);
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
}
