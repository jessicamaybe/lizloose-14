using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._UM.Drip.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class DripDrobeComponent : Component
{

    /// <summary>
    /// Sound that plays when ejecting an item
    /// </summary>
    [DataField]
    public SoundSpecifier SoundVend = new SoundCollectionSpecifier("VendingDispense")
    {
        Params = new AudioParams
        {
            Volume = -4f,
            Variation = 0.15f
        }
    };

}

[Serializable, NetSerializable]
public enum DripDrobeUiKey : byte
{
    Key,
}


/// <summary>
/// Send by the client when trying to dispense an item inside the fridge.
/// </summary>
[Serializable, NetSerializable]
public sealed class DripDrobeDispenseItemMessage(EntProtoId protoId) : BoundUserInterfaceMessage
{
    public EntProtoId Item = protoId;
}
