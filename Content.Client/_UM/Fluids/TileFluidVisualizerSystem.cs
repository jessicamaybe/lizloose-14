using Content.Client._UM.Fluids.Components;
using Content.Client.IconSmoothing;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.FixedPoint;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed class TileFluidVisualsSystem : VisualizerSystem<TileFluidVisualsComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IconSmoothSystem _iconSmooth = default!;

    protected override void OnAppearanceChange(EntityUid uid, TileFluidVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<IconSmoothComponent>(uid, out var smooth))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, FluidColorVisuals.Volume, out var flooded, args.Component))
        {
            if (flooded && !smooth.AdditionalKeys.Contains("walls"))
            {
                smooth.AdditionalKeys.Add("walls");
                _iconSmooth.DirtyNeighbours(uid);
            }

            if (!flooded && smooth.AdditionalKeys.Contains("walls"))
            {
                smooth.AdditionalKeys.Remove("walls");
                _iconSmooth.DirtyNeighbours(uid);
            }
        }

        if (AppearanceSystem.TryGetData<Color>(uid, FluidColorVisuals.Color, out var color, args.Component))
        {
            var layer = sprite[FluidColorVisuals.Color];
            layer.Color = color;

            if (!SpriteSystem.TryGetLayer((uid, args.Sprite), 0, out var spriteLayer, false))
                return;

            var rand = _random.Next(0, 100);

            if (spriteLayer.State == $"{smooth.StateBase}15" && rand < 13)
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, $"{smooth.StateBase}15-lines");
                return;
            }



            if (spriteLayer.State == $"{smooth.StateBase}15-lines")
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, $"{smooth.StateBase}15");
        }
    }
}
