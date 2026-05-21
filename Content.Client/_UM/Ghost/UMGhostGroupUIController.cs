using Content.Client._UM.UserInterface.Controls;
using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._UM.Ghost;
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
    [UISystemDependency] private readonly SpriteSystem _sprite = default!;

    private LayoutContainer _pikminIndicatorRoot = default!;

    private LayoutContainer _actionPickerRoot = default!;

    private Dictionary<EntityUid, Control> _activeIndicators = new();

    private Dictionary<EntityUid, GhostVoteContainer> _activeVotes = new();

    public override void Initialize()
    {
        base.Initialize();
        _pikminIndicatorRoot = new LayoutContainer();
        _actionPickerRoot = new LayoutContainer();

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

        _actionPickerRoot.Orphan();
        root.AddChild(_actionPickerRoot);
        LayoutContainer.SetAnchorPreset(_actionPickerRoot, LayoutContainer.LayoutPreset.Wide);
        _actionPickerRoot.SetPositionLast();
    }

    public void OpenActionPicker(NetEntity ent, List<GhostGroupAction> actions)
    {
        var radial = new SimpleRadialMenu();
        radial.OpenOverMouseScreenPosition();
        var buttons = ConvertToButtons(ent, actions);

        radial.SetButtons(buttons);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(NetEntity ent, List<GhostGroupAction> actions)
    {
        var buttons = new List<RadialMenuOptionBase>();

        foreach (var action in actions)
        {
            var option = new RadialMenuActionOption<(NetEntity, GhostGroupAction)>(SendActionSelect, (ent, action))
            {
                IconSpecifier = RadialMenuIconSpecifier.With(action.Icon),
            };
            buttons.Add(option);
        }

        return buttons;
    }

    private void SendActionSelect((NetEntity, GhostGroupAction) action)
    {
        EntityManager.RaisePredictiveEvent(new RequestGhostActionVoteEvent(action.Item1, action.Item2));
    }


    public void UpdateActiveIndicators()
    {
        if (_state.CurrentState is not GameplayState)
        {
            _pikminIndicatorRoot.RemoveAllChildren();
            return;
        }

        List<EntityUid> removed = new();

        foreach (var (ent, control) in _activeVotes)
        {
            if (!_ent.TryGetComponent<UMGhostGroupInteractableComponent>(ent, out var comp) || comp.Votes.Count == 0)
            {
                if (control.Parent != null)
                    _pikminIndicatorRoot.RemoveChild(control.Parent);
                removed.Add(ent);
                continue;
            }

            var sorted = new SortedDictionary<GhostGroupAction, List<EntityUid>>();

            foreach (var (user, action) in comp.Votes)
            {
                if (!sorted.TryGetValue(action, out var list))
                    sorted[action] = list = new List<EntityUid>();
                list.Add(user);
            }

            control.UpdateVote(sorted, comp.Amount);
        }

        foreach (var remove in removed)
        {
            _activeVotes.Remove(remove);
        }
    }


    public void CreateIndicators()
    {
        var query = _ent.EntityQueryEnumerator<UMGhostGroupInteractableComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Votes.Count == 0)
                continue;

            if (!_activeVotes.TryGetValue(uid, out var voteContainer))
            {
                var entityControl = new EntityControl(uid, -1.5f);
                _pikminIndicatorRoot.AddChild(entityControl);
                _activeVotes[uid] = new GhostVoteContainer();
                voteContainer = _activeVotes[uid];
                entityControl.AddChild(voteContainer);
            }

            var sorted = new SortedDictionary<GhostGroupAction, List<EntityUid>>();

            foreach (var (user, action) in comp.Votes)
            {
                if (!sorted.TryGetValue(action, out var list))
                    sorted[action] = list = new List<EntityUid>();
                list.Add(user);
            }

            voteContainer.UpdateVote(sorted, comp.Amount);
        }
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (_state.CurrentState is not GameplayState)
        {
            _pikminIndicatorRoot.RemoveAllChildren();
            return;
        }

        CreateIndicators();
        UpdateActiveIndicators(); //TODO: Make this less shit, also need to do cooldown timer
    }
}
