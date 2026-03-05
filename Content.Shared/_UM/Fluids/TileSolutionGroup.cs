namespace Content.Shared._UM.Fluids;

public sealed class TileSolutionGroup
{
    [ViewVariables]
    public readonly List<TileSolution> Tiles = new(100);

    [ViewVariables]
    public bool Disposed = false;

    [ViewVariables]
    public int BreakdownCooldown = 0;

}
