using Content.Shared.FixedPoint;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class FluidDeleteComponent : Component
{
    [DataField]
    public FixedPoint2 Amount = 30;
}
