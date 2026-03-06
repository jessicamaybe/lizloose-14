using System.Numerics;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._UM.Fluids.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GridFluidVisualsComponent : Component
{
    /// <summary>
    /// The tiles that have had their solution data updated since last tick
    /// </summary>
    public readonly HashSet<Vector2i> InvalidTiles = new();

    /// <summary>
    /// For normal puddles only.
    /// Since players can only interact with normal puddles (low height, etc), we draw these as entities.
    /// </summary>
    [ViewVariables]
    public Dictionary<Vector2i, EntityUid> DrawnTiles = new();
}

[Serializable, NetSerializable]
public sealed class TileSolutionState(Vector2i indices, Color color, FixedPoint2 volume)
{
    public Vector2i Indices = indices;

    public Color Color = color;

    public FixedPoint2 Volume = volume;
}


[Serializable, NetSerializable]
public enum FluidColorVisuals
{
    Color,
}
