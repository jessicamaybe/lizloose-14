using Content.Client._UM.Fluids.Components;
using Content.Shared._UM.Fluids.Components;
using Robust.Client.GameObjects;

namespace Content.Client._UM.Fluids;

/// <summary>
/// This handles...
/// </summary>
public sealed class TileFluidVisualsSystem : VisualizerSystem<TileFluidVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, TileFluidVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite)
            && AppearanceSystem.TryGetData<Color>(uid, FluidColorVisuals.Color, out var color, args.Component))
        {
            var layer = sprite[FluidColorVisuals.Color];
            layer.Color = color;
        }
    }
}
