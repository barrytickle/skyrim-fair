using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The last of Holidays (<see cref="HolidaysFreeConfig"/>): with the paper lanterns, this leaves
/// SkyrimFair.esp needing only Skyrim.esm. Built in the lanterns' FormID range, after them.
/// - The bunting: vanilla's festival line with the fair's own colourways (tools/make_bunting.py),
///   a texture set and a static per scheme, each pennant (the NIF's Flags shapes) swapped.
/// - The props: each Holidays piece changes base to a vanilla or fair piece (a silver platter on
///   a static of vanilla's own mesh; a basket, a crate, a sign post), and gains what made it
///   (apples in the basket, bottles on the crate, the sign's board) as new references beside it.
///
/// Every placed Holidays piece keeps its FormID and only changes base, last of all.
/// </summary>
internal static class FairHolidaysFree
{
    private const float Deg = MathF.PI / 180f;

    public static (int Bunting, int Props, int Added) Build(SkyrimMod mod, HolidaysFreeConfig config, ISkyrimModGetter master)
    {
        var byEditorId = mod.Statics.Where(s => s.EditorID is not null).ToDictionary(s => s.EditorID!, s => s.FormKey);
        FormKey Piece(string piece) => piece.StartsWith('@')
            ? byEditorId.TryGetValue($"{config.PropPrefix}{piece[1..]}", out var k) ? k : throw new InvalidOperationException($"holidaysFree: no prop {piece}")
            : FormKeyHelper.Parse(piece);

        // The bunting: a texture set and a static per scheme.
        var bunting = config.Bunting;
        var ropeFrom = master.Statics.First(s => s.FormKey == FormKeyHelper.Parse(bunting.BoundsFrom));
        var schemes = new Dictionary<string, FormKey>();
        foreach (var scheme in bunting.Replace.Values.Distinct().OrderBy(s => s, StringComparer.Ordinal))
        {
            var id = char.ToUpperInvariant(scheme[0]) + scheme[1..];
            var set = new TextureSet(mod)
            {
                EditorID = $"{bunting.EditorIdPrefix}{id}Flags",
                ObjectBounds = new ObjectBounds(),
                Diffuse = string.Format(bunting.Texture, scheme),
                NormalOrGloss = bunting.Normal,
                Flags = 0,
            };
            mod.TextureSets.Add(set);
            var stat = new Static(mod)
            {
                EditorID = $"{bunting.EditorIdPrefix}{id}",
                ObjectBounds = ropeFrom.ObjectBounds.DeepCopy(),
                Model = new Model { File = bunting.Model },
            };
            stat.Model.AlternateTextures = bunting.Flags
                .Select(f => new AlternateTexture { Name = f.Name, Index = f.Index, NewTexture = new FormLink<ITextureSetGetter>(set.FormKey) })
                .ToExtendedList();
            mod.Statics.Add(stat);
            schemes[scheme] = stat.FormKey;
        }

        // Statics the props need that neither vanilla nor the fair has (vanilla's silver platter is an item).
        foreach (var s in config.Statics)
        {
            var from = master.EnumerateMajorRecords().First(r => r.FormKey == FormKeyHelper.Parse(s.From));
            var model = from switch
            {
                IMiscItemGetter m => m.Model,
                IStaticGetter st => st.Model,
                _ => throw new InvalidOperationException($"holidaysFree: {s.From} has no model"),
            };
            var stat = new Static(mod)
            {
                EditorID = s.EditorId,
                ObjectBounds = ((IObjectBoundedGetter)from).ObjectBounds.DeepCopy(),
                Model = new Model { File = model!.File.GivenPath },
            };
            mod.Statics.Add(stat);
            byEditorId[s.EditorId] = stat.FormKey;
        }

        // Every placed Holidays piece, in FormID order, with the cell it's in (for its extras).
        var ropes = bunting.Replace.ToDictionary(kv => FormKeyHelper.Parse(kv.Key), kv => schemes[kv.Value]);
        var props = config.Props.ToDictionary(p => FormKeyHelper.Parse(p.Replace));
        var placed = mod.Worldspaces
            .SelectMany(w => w.SubCells.SelectMany(b => b.Items).SelectMany(sb => sb.Items)
                .SelectMany(c => c.Temporary.OfType<PlacedObject>().Select(o => (Cell: c, Ref: o)))
                .Concat(w.TopCell is { } top
                    ? top.Persistent.OfType<PlacedObject>().Select(o => (Cell: top, Ref: o))
                    : Enumerable.Empty<(Cell Cell, PlacedObject Ref)>()))
            .Where(x => x.Ref.Placement is not null && (ropes.ContainsKey(x.Ref.Base.FormKey) || props.ContainsKey(x.Ref.Base.FormKey)))
            .OrderBy(x => x.Ref.FormKey.ID)
            .ToList();
        var (nBunting, nProps, added) = (0, 0, 0);
        foreach (var (cell, o) in placed)
        {
            if (ropes.TryGetValue(o.Base.FormKey, out var rope))
            {
                o.Base = new FormLinkNullable<IPlaceableObjectGetter>(rope);
                nBunting++;
                continue;
            }

            var prop = props[o.Base.FormKey];
            var at = o.Placement!;
            var scale = o.Scale ?? 1f;
            o.Base = new FormLinkNullable<IPlaceableObjectGetter>(prop.Base.StartsWith('=') ? byEditorId[prop.Base[1..]] : Piece(prop.Base));
            o.Scale = MathF.Abs(scale * prop.Scale - 1f) < 1e-4f ? null : scale * prop.Scale;
            if (prop.Z != 0f)
            {
                at.Position = new P3Float(at.Position.X, at.Position.Y, at.Position.Z + prop.Z * scale);
            }

            nProps++;
            var yaw = at.Rotation.Z;
            var (rx, ry) = (MathF.Cos(yaw), -MathF.Sin(yaw));
            var (fx, fy) = (MathF.Sin(yaw), MathF.Cos(yaw));
            foreach (var w in prop.With)
            {
                var s = scale * prop.Scale;
                var extra = new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(Piece(w.Piece)),
                    Scale = MathF.Abs(s * w.Scale - 1f) < 1e-4f ? null : s * w.Scale,
                    Placement = new Placement
                    {
                        Position = new P3Float(
                            at.Position.X + (rx * w.X + fx * w.Y) * s,
                            at.Position.Y + (ry * w.X + fy * w.Y) * s,
                            at.Position.Z + w.Z * s),
                        Rotation = new P3Float(0f, 0f, yaw + w.Yaw * Deg),
                    },
                };
                if (cell.Persistent.Contains(o))
                {
                    cell.Persistent.Add(extra);
                }
                else
                {
                    cell.Temporary.Add(extra);
                }

                added++;
            }
        }

        return (nBunting, nProps, added);
    }
}
