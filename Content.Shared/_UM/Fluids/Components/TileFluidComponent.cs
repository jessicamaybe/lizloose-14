using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TileFluidComponent : Component
{
    /// <summary>
    /// Indices of the tile we're on
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Vector2i Indices;

    //this is just here for some entity effects that need it
    public Solution? Solution;

    /// <summary>
    ///     Examine text for the amount of solution.
    /// </summary>
    [DataField]
    public LocId LocVolume = "examinable-solution-on-examine-volume-fluid";

    /// <summary>
    ///     Examine text for the physical description of the primary reagent.
    /// </summary>
    [DataField]
    public LocId LocPhysicalQuality = "shared-solution-container-component-on-examine-main-text";

    /// <summary>
    ///     Examine text for reagents that are obvious like water.
    /// </summary>
    [DataField]
    public LocId LocRecognizableReagents = "examinable-solution-has-recognizable-chemicals";
}



[Serializable, NetSerializable]
public enum FluidHeight
{
    Puddle, // less than 100
    Overflowing, // more than 100u
    WaistHeight, // more than 500u
    Flooded // more than 1000u
}
