using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Solution;
using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server._UM.Fluids.EntityEffects;

/// <summary>
/// Copy paste of AreaReactionEffect to work with puddles
/// </summary>
public sealed partial class PuddleAreaReactionEntityEffectsSystem : EntityEffectSystem<TileFluidComponent, AreaReactionEffect>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly GridFluidSystem _gridFluid = default!;

    protected override void Effect(Entity<TileFluidComponent> entity, ref EntityEffectEvent<AreaReactionEffect> args)
    {
        if (entity.Comp.Solution == null)
            return;

        var solution = entity.Comp.Solution;

        var xform = Transform(entity);
        var mapCoords = _xform.GetMapCoordinates(entity);
        var spreadAmount = (int) Math.Max(0, Math.Ceiling(args.Scale / args.Effect.OverflowThreshold));
        var effect = args.Effect;

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid) ||
            !_map.TryGetTileRef(gridUid, grid, xform.Coordinates, out var tileRef))
            return;

        if (_spreader.RequiresFloorToSpread(effect.PrototypeId.ToString()) && _turf.IsSpace(tileRef))
            return;

        var coords = _map.MapToGrid(gridUid, mapCoords);
        var ent = Spawn(args.Effect.PrototypeId, coords.SnapToGrid());

        _smoke.StartSmoke(ent, solution, args.Effect.Duration, spreadAmount);

        _audio.PlayPvs(args.Effect.Sound, entity, AudioParams.Default.WithVariation(0.25f));
    }
}

