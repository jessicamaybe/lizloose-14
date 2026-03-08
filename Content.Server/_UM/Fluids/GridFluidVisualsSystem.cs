using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed class GridFluidVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly GridFluidSystem _gridFluid = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        ProcessGridVisuals();
    }

    public void MarkInvalid(Entity<GridFluidComponent> ent, Vector2i indices)
    {
        var fluidVisuals = EnsureComp<GridFluidVisualsComponent>(ent);

        fluidVisuals.InvalidTiles.Add(indices);
    }

    public void MarkInvalid(EntityUid ent, Vector2i indices)
    {
        var fluidVisuals = EnsureComp<GridFluidVisualsComponent>(ent);

        fluidVisuals.InvalidTiles.Add(indices);
    }

    private void ProcessGridVisuals()
    {
        var query = AllEntityQuery<GridFluidVisualsComponent, GridFluidComponent, MapGridComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var visuals, out var gridFluid, out var grid, out var meta))
        {
            var ent = (uid, visuals, gridFluid, grid, meta);
            CheckDeleted(ent);

            foreach (var index in visuals.InvalidTiles)
            {
                UpdateTileVisuals(ent, index);
            }
            visuals.InvalidTiles.Clear();
        }
    }

    private void CheckDeleted(Entity<GridFluidVisualsComponent, GridFluidComponent, MapGridComponent, MetaDataComponent> ent)
    {
        var gridFluid = ent.Comp2;
        var gridVisuals = ent.Comp1;
        var deleted = new List<Vector2i>();

        foreach (var (indices, tileEnt) in gridVisuals.DrawnTiles)
        {
            if (!_gridFluid.TryGetTileSolution(gridFluid, indices, out _))
            {
                Log.Debug("Deleted sprite");
                QueueDel(tileEnt);
                deleted.Add(indices);
            }
        }
        foreach (var tiled in deleted)
        {
            gridVisuals.DrawnTiles.Remove(tiled);
            gridVisuals.InvalidTiles.Remove(tiled);
        }
    }

    private void UpdateTileVisuals(Entity<GridFluidVisualsComponent, GridFluidComponent, MapGridComponent, MetaDataComponent> ent, Vector2i indices)
    {
        var gridFluid = ent.Comp2;
        var gridVisuals = ent.Comp1;
        var grid = ent.Comp3;

        if (!_gridFluid.TryGetTileSolution(gridFluid, indices, out var tileSolution))
        {
            RemoveTileVisuals(ent, indices);
            return;
        }

        if (!gridVisuals.DrawnTiles.TryGetValue(indices, out var drawnEnt))
        {
            var coords = _map.GridTileToLocal(ent.Owner, grid, indices);
            drawnEnt = Spawn("FluidTest25", coords);
            var relay = EnsureComp<TileSolutionRelayComponent>(drawnEnt);
            relay.TileSolution = tileSolution;
            gridVisuals.DrawnTiles.Add(indices, drawnEnt);
        }
        if (!TryComp(drawnEnt, out AppearanceComponent? appearance))
            return;

        SetPuddleColor(drawnEnt, appearance, tileSolution.Solution.GetColor(_prototypeManager), tileSolution.Solution.Volume);
        SetPuddleVolume(drawnEnt, appearance, tileSolution.Solution.Volume);
    }

    private void RemoveTileVisuals(Entity<GridFluidVisualsComponent, GridFluidComponent, MapGridComponent, MetaDataComponent> ent,
        Vector2i indices)
    {
        var gridFluid = ent.Comp2;
        var gridVisuals = ent.Comp1;

        if (gridVisuals.DrawnTiles.TryGetValue(indices, out var drawnEnt))
        {
            QueueDel(drawnEnt);
        }
    }

    private void SetPuddleVolume(EntityUid uid, AppearanceComponent appearance, FixedPoint2 volume)
    {
        var flooded = false;
        if (volume > 75)
            flooded = true;

        if (_appearance.TryGetData<bool>(uid, FluidColorVisuals.Volume, out var currentFloodStatus))
        {
            if (currentFloodStatus == flooded) //We were full before, and we still are
                return;
        }

        _appearance.SetData(uid, FluidColorVisuals.Volume, flooded, appearance);
    }

    private void SetPuddleColor(EntityUid uid, AppearanceComponent appearance, Color color, FixedPoint2 volume)
    {
        var maxOpacity = 200;
        var opacity = Math.Clamp(volume.Value/10, 100, maxOpacity);

        // convert to float ratio and then to byte
        color = color.WithAlpha((byte)((opacity / (float)maxOpacity) * 200));

        if (_appearance.TryGetData<Color>(uid, FluidColorVisuals.Color, out var currentColor))
        {
            var diff = Math.Abs(currentColor.ToArgb() - color.ToArgb());
            if (diff < 500)
                return;
        }

        _appearance.SetData(uid, FluidColorVisuals.Color, color, appearance);
    }

    /// <summary>
    /// Move tile to a new grid
    /// </summary>
    /// <param name="oldGrid"></param>
    /// <param name="newGrid"></param>
    /// <param name="tile"></param>
    public void MoveTile(GridFluidComponent oldGrid, Entity<GridFluidComponent> newGrid, TileSolution tile)
    {
        if (!TryComp<GridFluidVisualsComponent>(tile.GridIndex, out var oldGridVisuals))
            return;

        var newGridVisuals = EnsureComp<GridFluidVisualsComponent>(newGrid.Owner);
        if (oldGridVisuals.DrawnTiles.Remove(tile.GridIndices, out var ent))
            newGridVisuals.DrawnTiles.Add(tile.GridIndices, ent);

        newGridVisuals.InvalidTiles.Add(tile.GridIndices);
        oldGridVisuals.InvalidTiles.Remove(tile.GridIndices);
    }

}
