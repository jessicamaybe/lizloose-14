using System.Numerics;
using Content.Shared._UM.Ghost.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._UM.Ghost;

/// <summary>
/// This handles...
/// </summary>
public sealed class UMGhostGroupInteractableSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public void EnableOverlay()
    {
        _overlay.AddOverlay(new UMGhostInteractionOverlay(EntityManager, _eyeManager, _timing, _cache));
    }

    public void RemoveOverlay()
    {
        _overlay.RemoveOverlay<UMGhostInteractionOverlay>();
    }
}

public sealed class UMGhostInteractionOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transformSystem;
    private readonly IEyeManager _eyeManager;
    private readonly IGameTiming _timing;

    private readonly Font _font;

    public UMGhostInteractionOverlay(IEntityManager entManager, IEyeManager eyeManager, IGameTiming timing, IResourceCache cache)
    {
        _entManager = entManager;
        _eyeManager = eyeManager;
        _timing = timing;
        _transformSystem = _entManager.System<SharedTransformSystem>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 15);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entManager.EntityQueryEnumerator<UMGhostGroupInteractableComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var ghostInteractable, out var xform))
        {
            if (ghostInteractable.CurrentGhosts.Count == 0)
                continue;

            var (worldPos, worldRot) = _transformSystem.GetWorldPositionRotation(xform);

            if (!args.WorldAABB.Contains(worldPos))
                continue;

            var text = ghostInteractable.CurrentGhosts.Count + "/" + ghostInteractable.Amount;

            var dimensions = args.ScreenHandle.GetDimensions(_font, text, 1);

            var position = _eyeManager.WorldToScreen(worldPos) - new Vector2(dimensions.X /2, dimensions.Y * 2.25f);

            if (ghostInteractable.LastActivated + ghostInteractable.Cooldown > _timing.CurTime)
            {
                var timeLeft = _timing.CurTime - (ghostInteractable.LastActivated + ghostInteractable.Cooldown);

                args.ScreenHandle.DrawString(_font, position, timeLeft.Seconds.ToString(), color: Color.IndianRed);
                continue;
            }

            args.ScreenHandle.DrawString(_font, position, ghostInteractable.CurrentGhosts.Count + "/" + ghostInteractable.Amount, color: Color.IndianRed);
        }


    }
}
