using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Glow at the fair's lanterns (<see cref="LightsConfig"/>; Barry: "it feels a bit dark"). The
/// Holidays lanterns on the palisade's pennant ropes and on the festival lines across the lanes
/// are models without a light. So, at a spaced-out few of them, a vanilla no-shadow light hangs
/// just under the lantern: Whiterun's firelight (WRFireLightNS, radius 768) along the walls,
/// its street light (WRLightFireStreet01, radius 512) over the lanes. No shadows, so they're cheap.
///
/// Built in its own FormID range, after everything else that places things.
/// </summary>
internal static class FairLights
{
    public static (int Wall, int Lanes) Build(SkyrimMod mod, LightsConfig config, IEnumerable<PlacedObject> placed,
        IReadOnlyList<(float X, float Y)> perimeter, IEnumerable<string> lanternBases, Action<PlacedObject> put)
    {
        var bases = lanternBases.Select(FormKeyHelper.Parse).ToHashSet();
        var lanterns = placed.Where(o => bases.Contains(o.Base.FormKey) && o.Placement is not null)
            .OrderBy(o => o.FormKey.ID)
            .Select(o => o.Placement!.Position)
            .ToList();

        float WallDistance(float x, float y)
        {
            var best = float.MaxValue;
            for (int i = 0, j = perimeter.Count - 1; i < perimeter.Count; j = i++)
            {
                var (ax, ay, bx, by) = (perimeter[j].X, perimeter[j].Y, perimeter[i].X, perimeter[i].Y);
                var l2 = (bx - ax) * (bx - ax) + (by - ay) * (by - ay);
                var t = Math.Clamp(((x - ax) * (bx - ax) + (y - ay) * (by - ay)) / l2, 0f, 1f);
                var (dx, dy) = (x - (ax + t * (bx - ax)), y - (ay + t * (by - ay)));
                best = MathF.Min(best, MathF.Sqrt(dx * dx + dy * dy));
            }

            return best;
        }

        int Hang(IEnumerable<P3Float> at, string light, float spacing, int max, string name)
        {
            var chosen = new List<P3Float>();
            foreach (var p in at)
            {
                if (chosen.Count >= max) break;
                if (chosen.Any(c => (c.X - p.X) * (c.X - p.X) + (c.Y - p.Y) * (c.Y - p.Y) < spacing * spacing)) continue;
                chosen.Add(p);
                put(new PlacedObject(mod)
                {
                    EditorID = $"{config.EditorIdPrefix}{name}{chosen.Count}",
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(light)),
                    Placement = new Placement { Position = new P3Float(p.X, p.Y, p.Z - config.Below), Rotation = new P3Float(0f, 0f, 0f) },
                });
            }

            return chosen.Count;
        }

        var wall = Hang(lanterns.Where(p => WallDistance(p.X, p.Y) <= config.WallBand), config.WallLight, config.WallSpacing, config.MaxWall, "Wall");
        var lanes = Hang(lanterns.Where(p => WallDistance(p.X, p.Y) > config.WallBand), config.LaneLight, config.LaneSpacing, config.MaxLanes, "Lane");
        return (wall, lanes);
    }
}
