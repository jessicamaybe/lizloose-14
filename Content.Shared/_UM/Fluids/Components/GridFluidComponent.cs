using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class GridFluidComponent : Component
{
    [ViewVariables]
    public List<EntityUid> Pools = new();

    [ViewVariables]
    public List<EntityUid> DeleteQueue = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;


    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(100);

}
