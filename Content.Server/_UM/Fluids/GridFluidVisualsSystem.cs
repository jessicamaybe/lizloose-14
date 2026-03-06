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
        SetPuddleColor(drawnEnt, tileSolution.Solution.GetColor(_prototypeManager), tileSolution.Solution.Volume);
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

    private void SetPuddleColor(EntityUid uid, Color color, FixedPoint2 volume)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

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
}
