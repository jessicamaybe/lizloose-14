using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._UM.Ghost;

[Serializable, NetSerializable]
public sealed class GhostGroupAction : IComparable
{
    public int Priority = 0;

    public SpriteSpecifier? Icon;

    [NonSerialized]
    public Action? Act;

    public Color Color;

    public int CompareTo(object? obj)
    {
        if (obj is not GhostGroupAction otherAction)
            return -1;

        if (Priority != otherAction.Priority)
            return otherAction.Priority - Priority;

        if (Color != otherAction.Color)
            return otherAction.Color.ToArgb() - Color.ToArgb();

        return string.Compare(Icon?.ToString(), otherAction.Icon?.ToString(), StringComparison.CurrentCulture);
    }
}
