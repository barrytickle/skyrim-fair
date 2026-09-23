using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;

namespace SkyrimFair.Generator;

/// <summary>
/// Compatibility patches: small light plugins that switch another mod's per-NPC spells off
/// inside the fair's worldspace, and nowhere else. Each named spell is overridden with
/// "GetInWorldspace SkyrimFairWorld == 0" put first on every effect, so the engine itself
/// keeps the effect inactive at the fair; no script has to run.
///
/// Why: some mods give every NPC a cloak or a per-effect script. At the fair's crowd
/// density that floods Papyrus (Stealth Detection Fixes' detection cloak on every NPC,
/// feeding Maximum Destruction's OnMagicEffectApply script on every human: about 190^2
/// events a pulse, 2 million queued, the VM frozen). A Papyrus fix can't win that race,
/// since its events wait in the same queue.
/// </summary>
internal static class FairPatches
{
    public static List<(string Plugin, int Spells, int Effects)> Build(FairConfig config, string outputDirectory, FormKey fairWorld)
    {
        var built = new List<(string, int, int)>();
        foreach (var patch in config.CompatPatches)
        {
            var source = Path.IsPathRooted(patch.Source) ? patch.Source : Path.Combine(FairPaths.ConfigDirectory, patch.Source);
            if (!File.Exists(source))
            {
                Console.WriteLine($"  compatibility patch {patch.Plugin}: skipped, {patch.Source} not found");
                continue;
            }

            using var other = SkyrimMod.CreateFromBinaryOverlay(source, SkyrimRelease.SkyrimSE);
            var mod = new SkyrimMod(ModKey.FromFileName(patch.Plugin), SkyrimRelease.SkyrimSE);
            mod.ModHeader.Author = config.Identity.Author;
            mod.ModHeader.Description = $"Skyrim Fair compatibility: {other.ModKey.FileName}'s per-NPC spells are off inside the fair's worldspace only.";
            mod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.Small;

            var effects = 0;
            foreach (var key in patch.Spells.Select(FormKeyHelper.Parse))
            {
                var spell = other.Spells.FirstOrDefault(s => s.FormKey == key)
                    ?? throw new InvalidOperationException($"compatPatches {patch.Plugin}: {key} is not a spell in {other.ModKey.FileName}");
                var copy = spell.DeepCopy();
                foreach (var effect in copy.Effects)
                {
                    var outside = new GetInWorldspaceConditionData { RunOnType = Condition.RunOnType.Subject };
                    outside.WorldspaceOrList.Link.SetTo(fairWorld);
                    effect.Conditions.Insert(0, new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 0f, Data = outside });
                    effects++;
                }

                mod.Spells.Add(copy);
            }

            // Masters in load order: the base game, the other mod's own masters, the other
            // mod, then the fair (for its worldspace).
            var order = new List<ModKey>
            {
                ModKey.FromFileName("Skyrim.esm"), ModKey.FromFileName("Update.esm"), ModKey.FromFileName("Dawnguard.esm"),
                ModKey.FromFileName("HearthFires.esm"), ModKey.FromFileName("Dragonborn.esm"),
            };
            foreach (var m in other.ModHeader.MasterReferences.Select(r => r.Master).Append(other.ModKey)
                         .Append(ModKey.FromFileName(config.PluginName)))
            {
                if (!order.Contains(m))
                {
                    order.Add(m);
                }
            }

            mod.BeginWrite
                .ToPath(Path.Combine(outputDirectory, patch.Plugin))
                .WithLoadOrder(order.ToArray())
                .WithNoDataFolder()
                .Write();
            built.Add((patch.Plugin, patch.Spells.Count, effects));
        }

        return built;
    }
}
