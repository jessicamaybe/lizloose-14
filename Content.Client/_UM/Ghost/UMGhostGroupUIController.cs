using System.Numerics;
using Content.Client._UM.UserInterface.Controls;
using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.Message;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._UM.Ghost.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._UM.Ghost;

public sealed partial class UMGhostGroupUIController : UIController
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IStateManager _state = default!;

    [UISystemDependency] private readonly GhostSystem? _ghost = default;
    [UISystemDependency] private readonly TransformSystem? _transform = default;

    private LayoutContainer _pikminIndicatorRoot = default!;

    private Dictionary<EntityUid, Control> _activeIndicators = new();

    public override void Initialize()
    {
        base.Initialize();
        _pikminIndicatorRoot = new LayoutContainer();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
    }

    public void OnScreenLoad()
    {
        var viewportContainer = UIManager.ActiveScreen!.FindControl<LayoutContainer>("ViewportContainer");
        SetGhostCounterRoot(viewportContainer);
    }

    public void SetGhostCounterRoot(LayoutContainer root)
    {
        _pikminIndicatorRoot.Orphan();
        root.AddChild(_pikminIndicatorRoot);
        LayoutContainer.SetAnchorPreset(_pikminIndicatorRoot, LayoutContainer.LayoutPreset.Wide);
        _pikminIndicatorRoot.SetPositionLast();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_state.CurrentState is not GameplayState)
        {
            _pikminIndicatorRoot.RemoveAllChildren();
            return;
        }

        List<EntityUid> removed = new();

        foreach (var (ent, control) in _activeIndicators)
        {
            if (!_ent.EntityExists(ent))
                     continue;

            if (!_ent.TryGetComponent<UMGhostGroupInteractableComponent>(ent, out var comp) || comp.CurrentGhosts.Count == 0)
            {
                     _pikminIndicatorRoot.RemoveChild(control);
                     removed.Add(ent);
                     continue;
            }

            foreach (var child in control.Children)
            {
                if (child is GhostGroupCounter {} counter)
                {
                    if (comp.LastActivated + comp.Cooldown > _timing.CurTime)
                    {
                        _pikminIndicatorRoot.RemoveChild(control);
                        removed.Add(ent);
                        continue;
                    }
                    counter.SetText(comp.CurrentGhosts.Count, comp.Amount);
                    continue;
                }

                if (child is OutlineRichTextLabel { } timer)
                {
                    if (comp.LastActivated + comp.Cooldown < _timing.CurTime)
                    {
                        _pikminIndicatorRoot.RemoveChild(control);
                        removed.Add(ent);
                        continue;
                    }

                    var timeLeft = (_timing.CurTime - (comp.LastActivated + comp.Cooldown)).Duration();

                    var msg = new FormattedMessage();
                    msg.PushTag(new MarkupNode("font",
                        new MarkupParameter("PikminCounter"),
                        new Dictionary<string, MarkupParameter>()
                        {
                            { "size", new MarkupParameter(18) }
                        }));
                    msg.PushColor(Color.Red);
                    msg.AddMarkupOrThrow(timeLeft.Seconds.ToString()); //hardcode em dash lolol
                    msg.Pop();
                    msg.Pop();
                    timer.SetMessage(msg, tagsAllowed: [typeof(FontTag), typeof(ColorTag)]);
                }
            }
        }
        foreach (var remove in removed)
        {
            _activeIndicators.Remove(remove);
        }

        var query = _ent.EntityQueryEnumerator<UMGhostGroupInteractableComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentGhosts.Count == 0)
                continue;

            if (!_activeIndicators.TryGetValue(uid, out var control))
            {
                if (comp.LastActivated + comp.Cooldown > _timing.CurTime)
                {
                    CreateTimer(uid, -1.0f);
                    continue;
                }
                CreateCounter(uid, -1.5f);
                continue;
            }
        }
    }

    private EntityControl CreateCounter(EntityUid ent, float offset)
    {
        var newControl = new EntityControl(ent, offset);
        _pikminIndicatorRoot.AddChild(newControl);
        _activeIndicators.Add(ent, newControl);

        var counter = new GhostGroupCounter();
        newControl.AddChild(counter);

        return newControl;
    }


    private EntityControl CreateTimer(EntityUid ent, float offset)
    {
        var newControl = new EntityControl(ent, offset);
        _pikminIndicatorRoot.AddChild(newControl);
        _activeIndicators.Add(ent, newControl);

        var label = new OutlineRichTextLabel();
        label.HorizontalAlignment = Control.HAlignment.Center;
        label.VerticalAlignment = Control.VAlignment.Center;
        newControl.AddChild(label);

        return newControl;
    }
}
