using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The fair's own paper lanterns in place of Holidays' (<see cref="PaperLanternsConfig"/>):
/// Astra's tall and round lanterns (meshes\barry_paper_lanterns\), each in the six colours of
/// tools/make_lantern_colours.py. Per shape and colour, in its own FormID range:
/// - a texture set: the coloured paper as diffuse and as glow map (the NIF's Glow shader lights
///   the paper with it), and the shared normal
/// - a static: the shape's NIF, with that texture set swapped onto its paper (the shape
///   <see cref="PaperLanternsConfig.PaperShape"/>, 3D index <see cref="PaperLanternsConfig.PaperIndex"/>)
///
/// Every placed Holidays lantern keeps its FormID and only changes base, last of all (after
/// the navmesh and the sight table, and the lights at the lanterns): the same colour, tall or
/// round by a hash of its FormID, scaled to Holidays' size and hung from the same point.
/// </summary>
internal static class FairPaperLanterns
{
    public sealed record Swap(IPlacedObject Ref, FormKey Base, float Scale, float Lift);

    public static List<Swap> Build(SkyrimMod mod, PaperLanternsConfig config, IEnumerable<IPlacedObject> placed)
    {
        // The records: a texture set and a static per shape and colour.
        var statics = new Dictionary<(string Shape, string Colour), FormKey>();
        foreach (var shape in config.Shapes)
        {
            foreach (var colour in config.Colours)
            {
                var id = $"{char.ToUpperInvariant(shape.Id[0])}{shape.Id[1..]}{char.ToUpperInvariant(colour[0])}{colour[1..]}";
                var paper = string.Format(config.PaperTexture, shape.Id, colour);
                var set = new TextureSet(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}{id}Paper",
                    ObjectBounds = new ObjectBounds(),
                    Diffuse = paper,
                    NormalOrGloss = config.NormalTexture,
                    GlowOrDetailMap = paper,
                    Flags = 0,
                };
                mod.TextureSets.Add(set);
                var stat = new Static(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}{id}",
                    ObjectBounds = new ObjectBounds
                    {
                        First = new P3Int16(shape.BoundsMin[0], shape.BoundsMin[1], shape.BoundsMin[2]),
                        Second = new P3Int16(shape.BoundsMax[0], shape.BoundsMax[1], shape.BoundsMax[2]),
                    },
                    Model = new Model { File = shape.Model },
                };
                stat.Model.AlternateTextures = new ExtendedList<AlternateTexture>
                {
                    new() { Name = config.PaperShape, Index = config.PaperIndex, NewTexture = new FormLink<ITextureSetGetter>(set.FormKey) },
                };
                mod.Statics.Add(stat);
                statics[(shape.Id, colour)] = stat.FormKey;
            }
        }

        // The swaps: every placed Holidays lantern, in FormID order.
        var replace = config.Replace.ToDictionary(kv => FormKeyHelper.Parse(kv.Key), kv => kv.Value);
        var swaps = new List<Swap>();
        foreach (var o in placed.Where(o => o.Placement is not null && replace.ContainsKey(o.Base.FormKey)).OrderBy(o => o.FormKey.ID))
        {
            var colour = replace[o.Base.FormKey];
            var shape = FairHash.Hash3((int)o.FormKey.ID, 7, 911) < config.TallShare ? config.Shapes[0] : config.Shapes[1];
            if (!statics.TryGetValue((shape.Id, colour), out var key))
            {
                throw new InvalidOperationException($"paperLanterns: no colour '{colour}' (replace {o.Base.FormKey})");
            }

            var scale = (o.Scale ?? 1f) * shape.Scale;
            swaps.Add(new Swap(o, key, scale, config.HookAbove * (o.Scale ?? 1f)));
        }

        return swaps;
    }

    /// <summary>The placed lanterns change base (their FormIDs stay), size and height.</summary>
    public static int Apply(List<Swap> swaps)
    {
        foreach (var s in swaps)
        {
            s.Ref.Base = new FormLinkNullable<IPlaceableObjectGetter>(s.Base);
            s.Ref.Scale = MathF.Abs(s.Scale - 1f) < 1e-4f ? null : s.Scale;
            var p = s.Ref.Placement!.Position;
            s.Ref.Placement.Position = new P3Float(p.X, p.Y, p.Z + s.Lift);
        }

        return swaps.Count;
    }
}
