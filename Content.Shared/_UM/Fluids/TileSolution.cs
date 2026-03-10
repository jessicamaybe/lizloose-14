using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._UM.Fluids;


[Serializable]
[DataDefinition]
public sealed partial class TileSolution
{
    [ViewVariables]
    [NonSerialized]
    public EntityUid GridIndex;

    [ViewVariables]
    public Vector2i GridIndices;

    /// <summary>
    /// The solution on this tile
    /// </summary>
    [ViewVariables]
    public Solution Solution;

    [ViewVariables]
    public Color Color;

    [ViewVariables]
    public FixedPoint2 Volume;

    [ViewVariables]
    public AtmosDirection BlockedDirections;

    public TileSolution(EntityUid gridIndex, Vector2i gridIndices)
    {
        GridIndex = gridIndex;
        GridIndices = gridIndices;
        Solution = new Solution(capacity: 100000);
    }
    public TileSolution(TileSolution other, bool copySolution = true)
    {
        GridIndex = other.GridIndex;
        GridIndices = other.GridIndices;
        Solution = copySolution ? other.Solution.Clone() : new Solution();
        Color = other.Solution.GetColor(null);
        Volume = other.Solution.Volume;
    }
}


public enum FillLevel
{
    Puddle,
    Ankle,
    Waist,
    Ceiling,
}
