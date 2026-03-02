using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Maps;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class GridFluidSystem : SharedGridFluidSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializePools();
        InitializeSource();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdatePools(frameTime);
    }


    private void AddPuddle(Entity<GridFluidComponent> ent)
    {


    }

}
