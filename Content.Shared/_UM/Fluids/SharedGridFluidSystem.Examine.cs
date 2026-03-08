using System.Linq;
using Content.Shared._UM.Fluids.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._UM.Fluids;

public abstract partial class SharedGridFluidSystem
{
    public void InitializeExamine()
    {
        SubscribeLocalEvent<TileFluidComponent, ExaminedEvent>(OnExamineTileFluid);
        SubscribeLocalEvent<TileFluidComponent, GetVerbsEvent<ExamineVerb>>(OnExamineTileVerb);
    }


    private void OnExamineTileVerb(Entity<TileFluidComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryGetSolution((ent, ent.Comp), out var solution))
            return;

        var scanEvent = new SolutionScanEvent();
        RaiseLocalEvent(args.User, scanEvent);
        if (!scanEvent.CanScan)
            return;

        var target = args.Target;
        var user = args.User;
        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = GetSolutionExamine(solution);
                _examineSystem.SendExamineTooltip(user, target, markup, false, false);
            },
            Text = Loc.GetString("scannable-solution-verb-text"),
            Message = Loc.GetString("scannable-solution-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/drink.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);

    }

    private FormattedMessage GetSolutionExamine(Solution solution)
    {
        var msg = new FormattedMessage();

        if (solution.Volume == 0)
        {
            msg.AddMarkupOrThrow(Loc.GetString("scannable-solution-empty-container"));
            return msg;
        }

        msg.AddMarkupOrThrow(Loc.GetString("scannable-solution-main-text"));

        var reagentPrototypes = solution.GetReagentPrototypes(_prototype);

        // Sort the reagents by amount, descending then alphabetically
        var sortedReagentPrototypes = reagentPrototypes
            .OrderByDescending(pair => pair.Value.Value)
            .ThenBy(pair => pair.Key.LocalizedName);

        foreach (var (proto, quantity) in sortedReagentPrototypes)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("scannable-solution-chemical"
                , ("type", proto.LocalizedName)
                , ("color", proto.SubstanceColor.ToHexNoAlpha())
                , ("amount", quantity)));
        }

        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("scannable-solution-temperature", ("temperature", Math.Round(solution.Temperature))));

        return msg;
    }

    /// <summary>
    /// I love copy and pasting from SolutionContainerSystem
    /// </summary>
    private void OnExamineTileFluid(Entity<TileFluidComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryGetSolution((ent, ent.Comp), out var solution))
            return;

        using (args.PushGroup(nameof(TileFluidComponent)))
        {
            var primaryReagent = solution.GetPrimaryReagentId();
            if (string.IsNullOrEmpty(primaryReagent?.Prototype) || !_prototype.Resolve<ReagentPrototype>(primaryReagent.Value.Prototype, out var primary))
                return;

            args.PushMarkup(Loc.GetString(ent.Comp.LocVolume,
                ("fillLevel", ExaminedVolume(ent, solution, args.Examiner)),
                ("current", solution.Volume),
                ("max", solution.MaxVolume)));

            var colorHex = solution.GetColor(_prototype).ToHexNoAlpha();

            args.PushMarkup(Loc.GetString(ent.Comp.LocPhysicalQuality,
                ("color", colorHex),
                ("desc", primary.LocalizedPhysicalDescription),
                ("chemCount", solution.Contents.Count)));

            var sortedReagentPrototypes = solution.GetReagentPrototypes(_prototype)
                .OrderByDescending(pair => pair.Value.Value)
                .ThenBy(pair => pair.Key.LocalizedName);

            var recognized = new List<string>();
            foreach (var keyValuePair in sortedReagentPrototypes)
            {
                var proto = keyValuePair.Key;
                if (!proto.Recognizable)
                {
                    continue;
                }

                recognized.Add(Loc.GetString("examinable-solution-recognized",
                    ("color", proto.SubstanceColor.ToHexNoAlpha()),
                    ("chemical", proto.LocalizedName)));
            }

            if (recognized.Count == 0)
                return;

            var msg = ContentLocalizationManager.FormatList(recognized);

            // Finally push the full message
            args.PushMarkup(Loc.GetString(ent.Comp.LocRecognizableReagents,
                ("recognizedString", msg)));
        }
    }

    public FluidHeight ExaminedVolume(Entity<TileFluidComponent> ent, Solution sol, EntityUid? examiner = null)
    {
        if (sol.Volume > 1000)
            return FluidHeight.Flooded;

        if (sol.Volume > 500)
            return FluidHeight.WaistHeight;

        if (sol.Volume > 100)
            return FluidHeight.Overflowing;

        return FluidHeight.Puddle;
    }
}
