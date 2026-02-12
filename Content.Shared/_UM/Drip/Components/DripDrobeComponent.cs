using Robust.Shared.Serialization;

namespace Content.Shared._UM.Drip.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class DripDrobeComponent : Component
{

}

[Serializable, NetSerializable]
public enum DripDrobeUiKey : byte
{
    Key,
}
