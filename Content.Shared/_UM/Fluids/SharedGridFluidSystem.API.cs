using System.Diagnostics.CodeAnalysis;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public partial class SharedGridFluidSystem
{

    /// <summary>
    /// Tries to get the solution on a given tile
    /// </summary>
    /// <param name="gridUid"></param>
    /// <param name="indices"></param>
    /// <param name="tileSolution"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryGetTileSolution(EntityUid gridUid, Vector2i indices, [NotNullWhen(true)] out TileSolution? tileSolution)
    {
        tileSolution = null;

        if (!TryComp<GridFluidComponent>(gridUid, out var gridFluidComponent))
            return false;

        if (!TryGetFluid(gridFluidComponent, indices, out var tile))
        {
            Log.Debug("No fluid?? wtf");
            return false;
        }

        tileSolution = tile;
        return true;
    }

    [PublicAPI]
    public bool TryGetTileSolution(GridFluidComponent gridFluid, Vector2i indices, [NotNullWhen(true)] out TileSolution? tileSolution)
    {
        tileSolution = null;

        if (!TryGetFluid(gridFluid, indices, out var tile))
            return false;

        tileSolution = tile;
        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="gridFluid"></param>
    /// <param name="indices"></param>
    /// <param name="tile"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryGetFluid(GridFluidComponent gridFluid,
        Vector2i indices,
        [NotNullWhen(true)] out TileSolution? tile)
    {
        tile = null;

        return gridFluid.Tiles.TryGetValue(indices, out tile);
    }

    public void AddFluid(Entity<TileFluidComponent?> ent, Solution solution)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var xform = Transform(ent);

        if (xform.GridUid is not { } grid || !TryGetTileSolution(grid, ent.Comp.Indices, out var tileSolution))
            return;

        tileSolution.Solution.AddSolution(solution, _prototype);
        DirtyTile((ent, ent.Comp));
    }


    public void MarkModifiedTile(GridFluidComponent gridFluid, Vector2i indices)
    {
        gridFluid.ModifiedTiles.Add(indices);
    }

    public bool TryGetSolution(Entity<TileFluidComponent?> ent, [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var xform = Transform(ent);

        if (xform.GridUid is not { } grid || !TryGetTileSolution(grid, ent.Comp.Indices, out var tileSolution))
        {
            Log.Debug("thist hing");
            return false;
        }

        solution = tileSolution.Solution;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Adds a solution to a tile
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="coords"></param>
    /// <param name="solution"></param>
    public void AddFluid(Entity<MapGridComponent> ent, EntityCoordinates coords, Solution solution)
    {
        if (!_map.TryGetTileRef(ent.Owner, ent.Comp, coords, out var tileRef))
            return;

        var gridFluid = EnsureComp<GridFluidComponent>(ent.Owner);

        AddFluid((ent.Owner, gridFluid), tileRef.GridIndices, solution);
    }

    public void AddFluid(Entity<GridFluidComponent> ent, Vector2i indices, Solution solution, bool active = true)
    {
        if (ent.Comp.Tiles.TryGetValue(indices, out var tile))
        {
            tile.Solution.AddSolution(solution, _prototype);
            AddTileReaction(ent.Comp, tile);
            MarkModifiedTile(ent.Comp, indices);
            if (active)
                AddActiveTile(ent.Comp, indices);
            return;
        }

        AddTile((ent.Owner, ent.Comp), indices, solution, active);
        MarkModifiedTile(ent.Comp, indices);
    }

    public virtual void AddTile(Entity<GridFluidComponent> ent, Vector2i indices, Solution solution, bool active = true)
    {
    }

    public virtual bool AddActiveTile(GridFluidComponent gridFluid, Vector2i indices)
    {
        return false;
    }

    public virtual bool AddActiveTile(GridFluidComponent gridFluid, TileSolution tile)
    {
        return false;
    }

    public virtual void AddTileReaction(GridFluidComponent gridFluid, TileSolution tile)
    {
    }
}
