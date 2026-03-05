using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._UM.Fluids;


[Serializable]
[DataDefinition]
public sealed partial class TileSolution
{
    [ViewVariables]
    public EntityUid GridIndex;

    [ViewVariables]
    public Vector2i GridIndices;

    /// <summary>
    /// The solution on this tile
    /// </summary>
    [ViewVariables]
    public Solution Solution;

    [ViewVariables]
    public FillLevel FillLevel;

    [ViewVariables]
    public FixedPoint2 ShareVolume;

    [ViewVariables]
    public FixedPoint2 LastShareVolume;

    [ViewVariables]
    public AtmosDirection BlockedDirections;

    public TileSolutionGroup? TileSolutionGroup;

    [ViewVariables]
    public bool Excited;
    public TileSolution(EntityUid gridIndex, Vector2i gridIndices)
    {
        GridIndex = gridIndex;
        GridIndices = gridIndices;
        Solution = new Solution(capacity: 100000);
    }

    public TileSolution(TileSolution other)
    {
        GridIndex = other.GridIndex;
        GridIndices = other.GridIndices;
        Solution = other.Solution;
    }
}


public enum FillLevel
{
    Puddle,
    Ankle,
    Waist,
    Ceiling,
}
