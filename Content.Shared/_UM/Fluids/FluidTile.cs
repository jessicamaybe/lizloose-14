using Content.Shared.Chemistry.Components;

namespace Content.Shared._UM.Fluids;


[Serializable]
[DataDefinition]
public sealed partial class TileSolution
{
    [ViewVariables]
    public EntityUid GridIndex;

    [ViewVariables]
    public Vector2i GridIndices;

    public Solution Solution;

    public TileSolution(EntityUid gridIndex, Vector2i gridIndices, Solution solution)
    {
        GridIndex = gridIndex;
        GridIndices = gridIndices;
        Solution = solution;
    }

    public TileSolution(TileSolution other)
    {
        GridIndex = other.GridIndex;
        GridIndices = other.GridIndices;
        Solution = other.Solution;
    }
}
