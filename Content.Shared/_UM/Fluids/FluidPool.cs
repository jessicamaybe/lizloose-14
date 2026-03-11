using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._UM.Fluids;


/// <summary>
/// If a bunch of puddles fill up so much that it's a "problem" we bunch them all up into a single pool.
/// Once inside a pool, there will be no reactions.
/// The fluid will try to spread until it can form back into normal puddles.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class FluidPool
{
    /// <summary>
    /// hashset of the indices that this pool takes up
    /// </summary>
    public HashSet<Vector2i> Indices = new();

    /// <summary>
    /// The current edges of this pool
    /// </summary>
    public Dictionary<Vector2i, List<Vector2i>> Edges = new();

    public Solution Solution;

    [ViewVariables]
    public Color Color;

    [ViewVariables]
    public FixedPoint2 Volume;

    public FluidPool()
    {
        Solution = new Solution(10000000);
    }
}
