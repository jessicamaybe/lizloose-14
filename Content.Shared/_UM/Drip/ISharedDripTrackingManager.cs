using Robust.Shared.Player;

namespace Content.Shared._UM.Drip;

public interface ISharedDripTrackingManager
{
    IReadOnlyDictionary<string, int> GetAvailableEntities(ICommonSession session);
}
