using Content.Shared._UM.Puddles.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Maps;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._UM.Puddles;

/// <summary>
/// This handles...
/// </summary>
public sealed class SpecialPuddleSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpecialPuddleComponent, SolutionContainerChangedEvent>(OnSolutionUpdate);
    }

    private void OnSolutionUpdate(Entity<SpecialPuddleComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.SolutionName)
            return;

        if (args.Solution.Volume <= 0)
            QueueDel(ent);

        if (args.Solution.Contents.Count == 1)
        {
            foreach (var content in args.Solution.Contents)
            {
                if (content.Reagent.Prototype == "Water")
                {
                    _puddle.TrySpillAt(ent.Owner, args.Solution, out var puddle, false);
                    QueueDel(ent);
                    break;
                }
            }
        }
    }

    private bool TryAddSolution(Entity<SpecialPuddleComponent?> puddle, Solution addedSolution)
    {
        if (!Resolve(puddle, ref puddle.Comp))
            return false;

        if (!TryComp<SolutionContainerManagerComponent>(puddle, out var sol))
            return false;

        _solution.EnsureAllSolutions((puddle, sol));

        if (!_solution.ResolveSolution(puddle.Owner, puddle.Comp.SolutionName, ref puddle.Comp.Solution))
            return false;

        _solution.AddSolution(puddle.Comp.Solution.Value, addedSolution);

        return true;
    }

    public void TrySpillAt(EntityUid uid, Solution solution, EntProtoId protoId)
    {
        TrySpillAt(uid, solution, protoId, out _);
    }

    public bool TrySpillAt(EntityUid uid, Solution solution, EntProtoId protoId, out EntityUid puddleUid)
    {
        Log.Debug("spill test0");

        if (solution.Volume <= 0)
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        var xform = Transform(uid);

        var gridUid = _transform.GetGrid(uid);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        var tileRef = _map.GetTileRef(gridUid.Value, mapGrid, xform.Coordinates);

        Log.Debug("spill test1");

        if (tileRef.Tile.IsEmpty || _turf.IsSpace(tileRef))
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        var anchored = _map.GetAnchoredEntitiesEnumerator(tileRef.GridUid, mapGrid, tileRef.GridIndices);
        var puddleQuery = GetEntityQuery<SpecialPuddleComponent>();

        while (anchored.MoveNext(out var ent))
        {
            if (!puddleQuery.TryGetComponent(ent, out var puddle))
                continue;

            if (TryAddSolution((ent.Value, puddle), solution))
            {
                puddleUid = ent.Value;
                return true;
            }
        }

        puddleUid = PredictedSpawnAtPosition(protoId, xform.Coordinates);
        var puddleComp = EnsureComp<SpecialPuddleComponent>(puddleUid);

        if (!TryAddSolution(puddleUid, solution))
        {
            QueueDel(puddleUid);
            puddleUid = EntityUid.Invalid;
            return false;
        }

        if (puddleComp.SpillSound != null)
            _audio.PlayPvs(puddleComp.SpillSound, puddleUid);

        return true;
    }
}
