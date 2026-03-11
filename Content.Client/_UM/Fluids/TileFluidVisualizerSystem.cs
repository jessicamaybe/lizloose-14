using Content.Client._UM.Fluids.Components;
using Content.Client.IconSmoothing;
using Content.Shared._UM.Fluids;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.FixedPoint;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed class TileFluidVisualsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IconSmoothSystem _iconSmooth = default!;
    [Dependency] private readonly SharedGridFluidSystem _gridFluid = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileFluidVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<TileFluidVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_appearance.TryGetData<Color>(ent, FluidColorVisuals.Color, out var color, args.Component))
            return;

        var layer = sprite[FluidColorVisuals.Color];
        layer.Color = color;

        if (!TryComp<IconSmoothComponent>(ent, out var smooth))
            return;

        smooth.AdditionalKeys.Add("walls");
        _iconSmooth.DirtyNeighbours(ent);
    }


    public override void Update(float frameTime)
    {
        var query = AllEntityQuery<TileFluidComponent, SpriteComponent, IconSmoothComponent>();

        while (query.MoveNext(out var uid, out var tileFluid, out var sprite, out var iconSmooth))
        {
            var xform = Transform(uid);

            if (xform.GridUid is not { } grid)
                continue;

            if (!TryComp<GridFluidComponent>(grid, out var gridFluid))
                continue;

            if (gridFluid.ModifiedTiles.Contains(tileFluid.Indices))
            {
                SetPuddleAppearance((uid, tileFluid, sprite, iconSmooth), gridFluid);
                gridFluid.ModifiedTiles.Remove(tileFluid.Indices);
            }
        }
    }

    private void SetPuddleAppearance(Entity<TileFluidComponent, SpriteComponent, IconSmoothComponent> ent, GridFluidComponent gridFluid)
    {
        var tile = ent.Comp1;
        var sprite = ent.Comp2;
        var smooth = ent.Comp3;

        if (!_gridFluid.TryGetFluid(gridFluid, tile.Indices, out var fluid))
            return;

        var color = fluid.Color;
        var volume = fluid.Volume;

        var maxOpacity = 200;
        var opacity = Math.Clamp(volume.Value/10, 100, maxOpacity);

        // convert to float ratio and then to byte
        color = color.WithAlpha((byte)((opacity / (float)maxOpacity) * 200));

        var layer = sprite[FluidColorVisuals.Color];
        layer.Color = color;


        if (volume > 75 && !smooth.AdditionalKeys.Contains("walls"))
        {
            smooth.AdditionalKeys.Add("walls");
            _iconSmooth.DirtyNeighbours(ent);
        }
        if (volume < 75 && smooth.AdditionalKeys.Contains("walls"))
        {
            smooth.AdditionalKeys.Remove("walls");
            _iconSmooth.DirtyNeighbours(ent);
        }
    }
}
