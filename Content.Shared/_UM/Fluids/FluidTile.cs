namespace Content.Shared._UM.Fluids;


[Serializable]
[DataDefinition]
public sealed partial class FluidTile
{
    [ViewVariables]
    public Vector2i GridIndices;

    [ViewVariables]
    public EntityUid GridIndex;

    public FluidTile(EntityUid gridIndex, Vector2i gridIndices)
    {
        GridIndex = gridIndex;
        GridIndices = gridIndices;
    }

    public FluidTile(FluidTile other)
    {
        GridIndex = other.GridIndex;
        GridIndices = other.GridIndices;
    }

    public FluidTile()
    {
    }
}
