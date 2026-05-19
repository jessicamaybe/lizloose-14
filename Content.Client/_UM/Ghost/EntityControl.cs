using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._UM.Ghost;

/// <summary>
/// Control that follows an entity with an offset
/// </summary>
public sealed class EntityControl : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly SharedTransformSystem _transformSystem;

    private EntityUid _entity;

    private float _offset;

    public EntityControl(EntityUid entity, float offset)
    {
        IoCManager.InjectDependencies(this);
        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _entity = entity;
        _offset = offset;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_entityManager.TryGetComponent<TransformComponent>(_entity, out var xform))
            return;

        var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * _offset;
        var worldPos = _transformSystem.GetWorldPosition(xform) + offset;

        var lowerCenter = _eyeManager.WorldToScreen(worldPos) / UIScale;
        var screenPos = lowerCenter - new Vector2(DesiredSize.X / 2, 0f);
        screenPos = (screenPos * 2).Rounded() / 2;
        LayoutContainer.SetPosition(this, screenPos);
    }
}
