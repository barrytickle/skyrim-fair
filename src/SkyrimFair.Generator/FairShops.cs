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
    public static (int Shops, int Keepers, int Items) Build(SkyrimMod mod, ShopsConfig config, string keeperPrefix, ISkyrimModGetter master)
    {
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
            foreach (var keyword in shop.Buys)
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
                VendorValues = new VendorValues { StartHour = 0, EndHour = 24, Radius = 0, OnlyBuysStolenItems = false, NotSellBuy = false },
                VendorLocation = new LocationTargetRadius { Target = new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf }, Radius = 0 },
            };
            // CRVA: every vanilla vendor faction has it (Mutagen leaves it out unless set); the
            // caravans' values, as theirs is the pattern. No ranks, as theirs.
            faction.CrimeValues = crimeFrom.CrimeValues?.DeepCopy();
            mod.Factions.Add(faction);
            shops++;

            // Its keepers: the trade's faction, and the merchants' job faction for the barter line.
            foreach (var npc in mod.Npcs.Where(n => (n.EditorID ?? "").StartsWith($"{keeperPrefix}{id}", StringComparison.Ordinal)
                && (n.EditorID ?? "").Length == keeperPrefix.Length + id.Length + 2))
            {
                npc.Factions.Add(new RankPlacement { Faction = new FormLink<IFactionGetter>(faction.FormKey), Rank = 0 });
                npc.Factions.Add(new RankPlacement { Faction = new FormLink<IFactionGetter>(merchantJob), Rank = 0 });
                keepers++;
            }
        }

        return (shops, keepers, items);
    }
}
