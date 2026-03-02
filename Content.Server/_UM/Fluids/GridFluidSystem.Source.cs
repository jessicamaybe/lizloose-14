using Content.Shared._UM.Fluids.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem
{
    private void InitializeSource()
    {
        SubscribeLocalEvent<FluidSourceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<FluidSourceComponent> ent, ref MapInitEvent args)
    {
        Log.Debug("Trying to spawn fluid source");

        var xform = Transform(ent);

        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var gridComponent))
            return;

        var pool = Spawn("FluidPool", xform.Coordinates);
        Log.Debug("Spawned pool");

        var poolComp = EnsureComp<FluidPoolComponent>(pool);
        poolComp.GridUid = xform.GridUid.Value;

        if (!_map.TryGetTileRef(xform.GridUid.Value, gridComponent, xform.Coordinates, out var tileRef))
        {
            Log.Debug("no tile ref? wtf?");
            return;
        }
        poolComp.Tiles.Add(tileRef.GridIndices);

        if (_solutionContainerSystem.ResolveSolution(pool,
                poolComp.SolutionName,
                ref poolComp.Solution,
                out _))
        {
            Log.Debug("Added solution to pool");
            _solutionContainerSystem.TryAddSolution(poolComp.Solution.Value, ent.Comp.Solution);
            poolComp.NeedsUpdate = true;
        }

    }
}
