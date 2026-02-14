using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;

namespace Content.Shared._UM.Puddles.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SpecialPuddleComponent : Component
{
    [DataField]
    public SoundSpecifier? SpillSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [DataField("solution")] public string SolutionName = "puddle";

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;
}
