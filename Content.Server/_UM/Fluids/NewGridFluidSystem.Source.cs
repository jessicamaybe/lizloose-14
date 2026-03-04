using Content.Shared._UM.Fluids.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class NewGridFluidSystem
{
    private void InitializeSource()
    {
        SubscribeLocalEvent<NewFluidSourceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NewFluidSourceComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);

        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid.Value, out var gridComponent))
            return;

        AddFluid((xform.GridUid.Value, gridComponent), xform.Coordinates, ent.Comp.Solution);
    }

}
