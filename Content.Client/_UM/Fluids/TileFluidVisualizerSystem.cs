using Content.Client._UM.Fluids.Components;
using Content.Client.IconSmoothing;
using Content.Shared._UM.Fluids.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed class TileFluidVisualsSystem : VisualizerSystem<TileFluidVisualsComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void OnAppearanceChange(EntityUid uid, TileFluidVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite)
            && AppearanceSystem.TryGetData<Color>(uid, FluidColorVisuals.Color, out var color, args.Component))
        {
            var layer = sprite[FluidColorVisuals.Color];
            layer.Color = color;

            if (!TryComp<IconSmoothComponent>(uid, out var smooth))
                return;
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
