using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Atmos;
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

    private void UpdatePools(float frameTime)
    {
        var query = EntityQueryEnumerator<FluidPoolComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NeedsUpdate)
            {
                UpdatePool((uid, comp));
                ShittyDraw((uid, comp));
            }
        }
    }

    private void OnSolutionChanged(Entity<FluidPoolComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.SolutionName)
            return;

        Log.Debug("Solution changed, updating pool: " + ent.Owner);
        UpdatePool(ent);
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

        var neighborTiles = GetAvailableNeighbors(ent);
        AddTiles(ent, neighborTiles);
        ent.Comp.NeedsUpdate = true;
    }

    private void ShittyDraw(Entity<FluidPoolComponent> ent)
    {
        foreach (var tile in ent.Comp.Tiles)
        {
            if (ent.Comp.DrawnTiles.ContainsKey(tile))
                continue;

            if (GetTileCoords(ent, tile, out var coords))
            {
                Log.Debug("Drawing at: " + coords);
                var spawned = Spawn("FluidTest75", coords.Value);
                ent.Comp.DrawnTiles.Add(tile, spawned);
            }
        }

        foreach (var tile in ent.Comp.DrawnTiles)
        {
            if (!ent.Comp.Tiles.Contains(tile.Key))
                QueueDel(tile.Value);
        }
    }

    private bool GetTileCoords(Entity<FluidPoolComponent> ent, Vector2i tile, [NotNullWhen(true)] out EntityCoordinates? coords)
    {
        coords = null;

        if (!TryComp<MapGridComponent>(ent.Comp.GridUid, out var gridComponent))
            return false;

        coords = _map.GridTileToLocal(ent.Comp.GridUid, gridComponent, tile);
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

    private void AddTiles(Entity<FluidPoolComponent> ent, List<Vector2i> tiles)
    {
        ent.Comp.Tiles.UnionWith(tiles);
    }

    private List<Vector2i> GetAvailableNeighbors(Entity<FluidPoolComponent> ent)
    {
        var airtightQuery = GetEntityQuery<AirtightComponent>();

        List<Vector2i> neighboringTiles = new();

        if (!TryComp<MapGridComponent>(ent.Comp.GridUid, out var gridComponent))
            return neighboringTiles;

        var gridXform = Transform(ent.Comp.GridUid);

        foreach (var tile in ent.Comp.Tiles)
        {
            //Get neighboring tiles that aren't in our pool
            for (var i = 0; i < 4; i++)
            {
                var atmosDir = (AtmosDirection)(1 << i);
                var neighborPos = tile.Offset(atmosDir);
                if (ent.Comp.Tiles.Contains(neighborPos))
                    continue;
                neighboringTiles.Add(neighborPos);
            }
        }
        List<Vector2i> unblockedNeighbors = new();
        foreach (var tile in neighboringTiles)
        {
            if (IsTileBlocked((ent.Comp.GridUid, gridComponent, gridXform), airtightQuery, tile))
                continue;
            unblockedNeighbors.Add(tile);
        }

        return unblockedNeighbors;
    }

    public bool IsTileBlocked(Entity<MapGridComponent, TransformComponent> ent, EntityQuery<AirtightComponent> airtightQuery, Vector2i tile)
    {
        var xform = ent.Comp2;
        if (xform.GridUid == null)
            return true;

        var anchored = _map.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, ent.Comp1, tile);

        var tileRef = _map.GetTileRef((ent.Owner, ent.Comp1), tile);

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
