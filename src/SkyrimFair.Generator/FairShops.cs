using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The stalls as shops (<see cref="ShopsConfig"/>): each trade's keeper sells what the stall
/// shows, the vanilla way. Per trade (Belethor's and the Khajiit caravans' records were the
/// pattern):
/// - a vendor faction (Vendor, CanBeOwner): open 0-24, the location NearSelf (as the caravans),
///   its merchant chest, and a buy list of the trade's own keywords (not inverted), so they buy
///   back little: "sell only" (Barry, 2026-09-25)
/// - a merchant chest (Respawns) in the fair's holding cell, with the trade's stock and a
///   little gold, restocking on vanilla's timer
/// Each keeper joins its trade's faction and JobMerchantFaction, whose line in DialogueGeneric's
/// OfferServicesTopic ("What have you got for sale?") opens the barter menu, in the keeper's own
/// vanilla voice.
///
/// Items are named by EditorID and resolved against the master, so a wrong name stops the build.
/// Built in its own FormID range, after everything else.
/// </summary>
internal static class FairShops
{
    /// <summary>
    /// Builds the shops. The counters' activators are made here (in the shops' range); the
    /// counter references change base in <see cref="Apply"/>, last of all, so nothing built from
    /// the statics (the navmesh, the sight table) sees a difference.
    /// </summary>
    public static (int Shops, int Keepers, int Items, List<(IPlacedObject Ref, FormKey Base)> Counters) Build(
        SkyrimMod mod, ShopsConfig config, string keeperPrefix, ISkyrimModGetter master, Worldspace world)
    {
        var keeperNpcs = new List<Npc>();
        var factionOf = new Dictionary<string, FormKey>(StringComparer.OrdinalIgnoreCase);
        var byEditorId = new Dictionary<string, FormKey>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in master.EnumerateMajorRecords())
        {
            if (r.EditorID is { } e && !byEditorId.ContainsKey(e))
            {
                byEditorId[e] = r.FormKey;
            }
        }

        FormKey Find(string editorId, string where) => byEditorId.TryGetValue(editorId, out var k)
            ? k
            : throw new InvalidOperationException($"shops {where}: no record '{editorId}' in {master.ModKey}");

        var holding = mod.Cells.Records.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells)
            .First(c => c.EditorID == config.HoldingCell);
        var merchantJob = FormKeyHelper.Parse(config.MerchantJobFaction);
        var crimeFrom = master.Factions.First(f => f.EditorID == config.FactionTemplate);
        // Vanilla's merchant chest (Belethor's): its model and bounds for ours.
        var template = master.Containers.First(c => c.EditorID == config.ChestTemplate);
        var shops = 0;
        var keepers = 0;
        var items = 0;
        foreach (var (theme, shop) in config.Trades.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            var id = char.ToUpperInvariant(theme[0]) + theme[1..];

            // The chest: its stock, and a little gold.
            var chest = new Container(mod)
            {
                EditorID = $"{config.EditorIdPrefix}{id}Chest",
                Name = "Merchant Chest",
                Flags = Container.Flag.Respawns,
                ObjectBounds = template.ObjectBounds.DeepCopy(),
                Model = template.Model?.DeepCopy(),
            };
            chest.Items = new ExtendedList<ContainerEntry>();
            foreach (var entry in shop.Stock.Append(new ShopItem { Item = config.Gold, Count = shop.Gold > 0 ? shop.Gold : config.GoldEach }))
            {
                chest.Items.Add(new ContainerEntry
                {
                    Item = new ContainerItem { Item = new FormLink<IItemGetter>(Find(entry.Item, theme)), Count = entry.Count },
                });
                items++;
            }

            mod.Containers.Add(chest);
            var chestRef = new PlacedObject(mod)
            {
                EditorID = $"{chest.EditorID}Ref",
                Base = new FormLinkNullable<IPlaceableObjectGetter>(chest.FormKey),
                Placement = new Placement { Position = new P3Float(shops * 100f, -2000f, 0f), Rotation = new P3Float(0f, 0f, 0f) },
                MajorRecordFlagsRaw = 0x400,  // persistent: the faction names it
            };
            holding.Persistent.Add(chestRef);

            // What they'll buy back: only their own kind of goods.
            var buys = new FormList(mod) { EditorID = $"{config.EditorIdPrefix}{id}Buys" };
            foreach (var keyword in config.TradeAnything ? new List<string> { config.NoSaleKeyword } : shop.Buys)
            {
                buys.Items.Add(new FormLink<ISkyrimMajorRecordGetter>(Find(keyword, theme)));
            }

            mod.FormLists.Add(buys);

            var faction = new Faction(mod)
            {
                EditorID = $"{config.EditorIdPrefix}{id}Faction",
                Name = $"{id} traders",
                Flags = Faction.FactionFlag.Vendor | Faction.FactionFlag.CanBeOwner,
                MerchantContainer = new FormLinkNullable<IPlacedObjectGetter>(chestRef.FormKey),
                VendorBuySellList = new FormLinkNullable<IFormListGetter>(buys.FormKey),
                // NotSellBuy: the list is what they won't trade (TradeAnything).
                VendorValues = new VendorValues { StartHour = 0, EndHour = 24, Radius = 0, OnlyBuysStolenItems = false, NotSellBuy = config.TradeAnything },
                VendorLocation = new LocationTargetRadius { Target = new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf }, Radius = 0 },
            };
            // CRVA: every vanilla vendor faction has it (Mutagen leaves it out unless set); the
            // caravans' values, as theirs is the pattern. No ranks, as theirs.
            faction.CrimeValues = crimeFrom.CrimeValues?.DeepCopy();
            mod.Factions.Add(faction);
            factionOf[theme] = faction.FormKey;
            shops++;

            // Its keepers: the trade's faction, and the merchants' job faction for the barter line.
            foreach (var npc in mod.Npcs.Where(n => (n.EditorID ?? "").StartsWith($"{keeperPrefix}{id}", StringComparison.Ordinal)
                && (n.EditorID ?? "").Length == keeperPrefix.Length + id.Length + 2))
            {
                npc.Factions.Add(new RankPlacement { Faction = new FormLink<IFactionGetter>(faction.FormKey), Rank = 0 });
                npc.Factions.Add(new RankPlacement { Faction = new FormLink<IFactionGetter>(merchantJob), Rank = 0 });
                keeperNpcs.Add(npc);
                keepers++;
            }
        }

        var byBase = keeperNpcs.ToDictionary(n => n.FormKey);
        var keeperRefs = world.EnumerateMajorRecords<IPlacedNpc>()
            .Where(r => r.Placement is not null && byBase.ContainsKey(r.Base.FormKey))
            .OrderBy(r => r.FormKey.ID)
            .ToList();

        // Keepers kept at their counters: each record is placed once, so its spot goes on the record.
        if (config.KeeperHome.Enabled)
        {
            foreach (var r in keeperRefs)
            {
                var npc = byBase[r.Base.FormKey];
                var entry = new ScriptEntry { Name = config.KeeperHome.Script };
                entry.Properties.Add(new ScriptFloatProperty { Name = "HomeX", Data = r.Placement!.Position.X });
                entry.Properties.Add(new ScriptFloatProperty { Name = "HomeY", Data = r.Placement.Position.Y });
                entry.Properties.Add(new ScriptFloatProperty { Name = "Stray", Data = config.KeeperHome.Stray });
                npc.VirtualMachineAdapter = new VirtualMachineAdapter { Version = 5, ObjectFormat = 2 };
                npc.VirtualMachineAdapter.Scripts.Add(entry);
            }
        }

        // The counters: an activator per trade and model, named for the stall.
        var counters = new List<(IPlacedObject Ref, FormKey Base)>();
        if (config.Counters.Enabled)
        {
            var models = config.Counters.Models.Select(FormKeyHelper.Parse).ToList();
            var made = new Dictionary<(string Trade, FormKey Model), Mutagen.Bethesda.Skyrim.Activator>();
            var pieces = world.EnumerateMajorRecords<IPlacedObject>()
                .Where(o => o.Placement is not null && models.Contains(o.Base.FormKey) && (o.MajorRecordFlagsRaw & 0x800) == 0)
                .OrderBy(o => o.FormKey.ID)
                .ToList();
            foreach (var piece in pieces)
            {
                var p = piece.Placement!.Position;
                var near = keeperRefs
                    .Select(k => (Ref: k, D: MathF.Sqrt((k.Placement!.Position.X - p.X) * (k.Placement.Position.X - p.X) + (k.Placement.Position.Y - p.Y) * (k.Placement.Position.Y - p.Y))))
                    .Where(k => k.D <= config.Counters.Reach)
                    .OrderBy(k => k.D)
                    .FirstOrDefault();
                if (near.Ref is null)
                {
                    continue;
                }

                var npc = byBase[near.Ref.Base.FormKey];
                var trade = npc.EditorID![keeperPrefix.Length..^2];
                if (!made.TryGetValue((trade, piece.Base.FormKey), out var act))
                {
                    var stat = master.Statics.First(x => x.FormKey == piece.Base.FormKey);
                    act = new Mutagen.Bethesda.Skyrim.Activator(mod)
                    {
                        EditorID = $"{config.EditorIdPrefix}{trade}Counter{made.Keys.Count(k => k.Trade == trade) + 1}",
                        Name = npc.Name?.String,
                        ObjectBounds = stat.ObjectBounds.DeepCopy(),
                        // The model path only: the static's MODT (texture hashes) on an
                        // activator crashed the game while loading (2026-09-25); many vanilla
                        // activators have none. PNAM and FNAM as vanilla's (Mutagen omits them).
                        Model = stat.Model?.DeepCopy(),
                        ActivateTextOverride = config.Counters.Verb,
                        MarkerColor = System.Drawing.Color.FromArgb(0, 0xCC, 0x4C, 0x33),
                        Flags = 0,
                    };
                    act.Model!.Data = null;  // no MODT
                    mod.Activators.Add(act);
                    made[(trade, piece.Base.FormKey)] = act;
                }

                // The keeper's base, not the reference: found at the counter when it's used.
                var entry = new ScriptEntry { Name = config.Counters.Script };
                entry.Properties.Add(new ScriptObjectProperty { Name = "Keeper", Object = new FormLink<ISkyrimMajorRecordGetter>(npc.FormKey) });
                entry.Properties.Add(new ScriptFloatProperty { Name = "Reach", Data = config.Counters.Reach + 60f });
                // Its keeper's own reference, looked up in game by FormID: the records' copies made
                // at load (leveled faces) aren't the record, so a search by base found nobody.
                entry.Properties.Add(new ScriptIntProperty { Name = "KeeperRefId", Data = (int)near.Ref.FormKey.ID });
                entry.Properties.Add(new ScriptStringProperty { Name = "KeeperPlugin", Data = mod.ModKey.FileName.String });
                piece.VirtualMachineAdapter = new VirtualMachineAdapter { Version = 5, ObjectFormat = 2 };
                piece.VirtualMachineAdapter.Scripts.Add(entry);
                counters.Add((piece, act.FormKey));
            }
        }

        // Fair prices: a hidden perk, vanilla Haggling's shape (PRKE entry point ModBuyPrices,
        // multiply, a condition on the perk owner), only inside the fair's worldspace. Appended.
        if (config.PriceMultiplier != 1f)
        {
            var perk = new Perk(mod)
            {
                EditorID = $"{config.EditorIdPrefix}Prices",
                Name = "Fair Prices",
                Description = "Everything at the fair costs more.",
                Trait = false,
                Level = 0,
                NumRanks = 1,
                Playable = false,
                Hidden = true,
            };
            var inFair = new GetInWorldspaceConditionData { RunOnType = Condition.RunOnType.Subject };
            inFair.WorldspaceOrList.Link.SetTo(world.FormKey);
            var effect = new PerkEntryPointModifyValue
            {
                Rank = 0,
                Priority = 0,
                EntryPoint = APerkEntryPointEffect.EntryType.ModBuyPrices,
                PerkConditionTabCount = 2,
                Modification = PerkEntryPointModifyValue.ModificationType.Multiply,
                Value = config.PriceMultiplier,
            };
            effect.Conditions.Add(new PerkCondition
            {
                RunOnTabIndex = 0,
                Conditions = { new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = inFair } },
            });
            // The merchant (tab 1, the speaker) in one of the priced trades' factions: OR'd.
            var priced = config.PricedTrades.Select(t => factionOf.TryGetValue(t, out var f)
                ? f
                : throw new InvalidOperationException($"shops pricedTrades: no trade '{t}'")).ToList();
            if (priced.Count > 0)
            {
                var speaker = new PerkCondition { RunOnTabIndex = 1 };
                for (var i = 0; i < priced.Count; i++)
                {
                    var inFaction = new GetInFactionConditionData { RunOnType = Condition.RunOnType.Subject };
                    inFaction.Faction.Link.SetTo(priced[i]);
                    speaker.Conditions.Add(new ConditionFloat
                    {
                        CompareOperator = CompareOperator.EqualTo,
                        ComparisonValue = 1f,
                        Flags = i < priced.Count - 1 ? Condition.Flag.OR : 0,
                        Data = inFaction,
                    });
                }

                effect.Conditions.Add(speaker);
            }

            perk.Effects.Add(effect);
            mod.Perks.Add(perk);
        }

        return (shops, keepers, items, counters);
    }

    /// <summary>The counters change base to their activators (their FormIDs stay).</summary>
    public static int Apply(List<(IPlacedObject Ref, FormKey Base)> counters)
    {
        foreach (var (piece, act) in counters)
        {
            piece.Base = new FormLinkNullable<IPlaceableObjectGetter>(act);
        }

        return counters.Count;
    }
}
