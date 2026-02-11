using Content.Shared._UM.Drip.Components;
using Content.Shared.Examine;

namespace Content.Shared._UM.Drip;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedTrackedDripSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TrackedDripComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<TrackedDripComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var str = Loc.GetString("drip-examine-text");
        args.PushMarkup(str);
    }
}
