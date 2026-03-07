using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for fluid sources that continually emit a fluid
/// (showers, flooded sinks, etc)
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ConstantFluidSourceComponent : Component
{
    /// <summary>
    /// If enabled, we're flowing
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// What solution we add every update
    /// </summary>
    [DataField, AutoNetworkedField]
    public Solution Solution = new([new("Water", 20)]);
}
