using Content.Shared._UM.Fluids.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed class ConstantFluidSourceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GridFluidSystem _gridFluid = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ConstantFluidSourceComponent>();
        while (query.MoveNext(out var uid, out var fluidSource))
        {
            if (fluidSource.NextUpdate > curTime)
                continue;

            fluidSource.NextUpdate += fluidSource.UpdateInterval;

            if (!fluidSource.Enabled)
                continue;

            var xform = Transform(uid);

            if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid.Value, out var gridComponent))
                return;

            _gridFluid.AddFluid((xform.GridUid.Value, gridComponent), xform.Coordinates, fluidSource.Solution);
        }

    }
}
