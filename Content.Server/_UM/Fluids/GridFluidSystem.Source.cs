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

        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid.Value, out var gridComponent))
        {
            Log.Debug("no whatever? wtf?");
            return;
        }

        AddFluid((xform.GridUid.Value, gridComponent), xform.Coordinates, ent.Comp.Solution);

    }
}
