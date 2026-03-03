using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem
{
    private void InitializePools()
    {
        SubscribeLocalEvent<FluidPoolComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(Entity<FluidPoolComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.SolutionName)
            return;

        ent.Comp.NeedsUpdate = true;
    }

    private void UpdatePool(Entity<FluidPoolComponent> ent)
    {
        Log.Debug("Trying to update pool: " + ent.Owner);
        if (ent.Comp.Tiles.Count == 0)
            return;

        if (!IsOverflowing(ent))
        {
            Log.Debug("pool is not overflowing");
            ent.Comp.NeedsUpdate = false;
            return;
        }

        if (ent.Comp.LastUpdateStuck)
        {
            ent.Comp.OverFlowLevel = 1;
            ent.Comp.NeedsUpdate = false;
            ShittyDraw(ent, true);
        }

        var neighborTiles = GetAvailableNeighbors(ent);
        if (neighborTiles.Count == 0)
        {
            //We're overflowing
            ent.Comp.LastUpdateStuck = true;
            return;
        }

        ent.Comp.LastUpdateStuck = false;

        if (ent.Comp.OverFlowLevel > 0)
        {
            ent.Comp.OverFlowLevel = 0;
            ShittyDraw(ent, true);
        }

        AddTiles(ent, neighborTiles);
        ent.Comp.NeedsUpdate = true;
    }

    private void ShittyDraw(Entity<FluidPoolComponent> ent, bool redraw = false)
    {
        if (redraw)
        {
            foreach (var tile in ent.Comp.DrawnTiles)
            {
               QueueDel(tile.Value);
            }
            ent.Comp.DrawnTiles.Clear();
        }

        foreach (var tile in ent.Comp.Tiles)
        {
            if (ent.Comp.DrawnTiles.ContainsKey(tile))
                continue;

            if (GetTileCoords(ent, tile, out var coords))
            {
                Log.Debug("Drawing at: " + coords);
                var proto = "FluidTest25";

                if (ent.Comp.OverFlowLevel == 1)
                    proto = "FluidTest50";

                var spawned = Spawn(proto, coords.Value);
                ent.Comp.DrawnTiles.Add(tile, spawned);
            }
        }

        foreach (var tile in ent.Comp.DrawnTiles)
        {
            if (!ent.Comp.Tiles.Contains(tile.Key))
                QueueDel(tile.Value);
        }
    }

    private bool GetTileCoords(Entity<FluidPoolComponent> ent, TileRef tile, [NotNullWhen(true)] out EntityCoordinates? coords)
    {
        coords = null;

        if (!TryComp<MapGridComponent>(tile.GridUid, out var gridComponent))
            return false;

        coords = _map.GridTileToLocal(ent.Comp.GridUid, gridComponent, tile.GridIndices);
        return true;
    }

    private bool IsOverflowing(Entity<FluidPoolComponent> ent)
    {
        var volume = CurrentVolume(ent);
        var amountPerTile = (volume / ent.Comp.Tiles.Count);

        Log.Debug("Current volume for pool: " + ent.Owner + "  is: " + volume + "  Amount per tile is: " + amountPerTile);
        return amountPerTile > ent.Comp.OverflowVolume;
    }

    private FixedPoint2 CurrentVolume(Entity<FluidPoolComponent> ent)
    {
        return _solutionContainerSystem.ResolveSolution(ent.Owner,
            ent.Comp.SolutionName,
            ref ent.Comp.Solution,
            out var solution)
            ? solution.Volume
            : FixedPoint2.Zero;
    }

    private void AddTiles(Entity<FluidPoolComponent> ent, List<TileRef> tiles)
    {
        ent.Comp.Tiles.UnionWith(tiles);
    }

    private List<TileRef> GetAvailableNeighbors(Entity<FluidPoolComponent> ent)
    {
        var airtightQuery = GetEntityQuery<AirtightComponent>();

        List<TileRef> neighboringTiles = new();

        if (!TryComp<MapGridComponent>(ent.Comp.GridUid, out var gridComponent))
            return neighboringTiles;

        var gridXform = Transform(ent.Comp.GridUid);

        foreach (var tile in ent.Comp.Tiles)
        {
            //Get neighboring tiles that aren't in our pool
            for (var i = 0; i < 4; i++)
            {
                var atmosDir = (AtmosDirection)(1 << i);
                var neighborPos = tile.GridIndices.Offset(atmosDir);
                if (!_map.TryGetTileRef(ent.Comp.GridUid, gridComponent, neighborPos, out var neighborTile))
                    continue;
                if (ent.Comp.Tiles.Contains(neighborTile))
                    continue;
                neighboringTiles.Add(neighborTile);
            }
        }
        List<TileRef> unblockedNeighbors = new();
        foreach (var tile in neighboringTiles)
        {
            if (IsTileBlocked((ent.Comp.GridUid, gridComponent, gridXform), airtightQuery, tile))
                continue;
            unblockedNeighbors.Add(tile);
        }

        return unblockedNeighbors;
    }

    public bool IsTileBlocked(Entity<MapGridComponent, TransformComponent> ent, EntityQuery<AirtightComponent> airtightQuery, TileRef tileRef)
    {
        var xform = ent.Comp2;
        if (xform.GridUid == null)
            return true;

        var anchored = _map.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, ent.Comp1, tileRef.GridIndices);

        //if (_turf.IsSpace(tileRef))
        //    return true;

        while (anchored.MoveNext(out var anchoredEnt))
        {
            if (airtightQuery.TryGetComponent(anchoredEnt, out var airtightComponent) && airtightComponent.AirBlocked)
                return true;
        }

        return false;
    }
}
