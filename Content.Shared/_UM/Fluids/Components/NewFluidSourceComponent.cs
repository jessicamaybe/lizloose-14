using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NewFluidSourceComponent : Component
{
    [DataField, AutoNetworkedField]
    public Solution Solution = new([new("Water", 1000)]);
}
