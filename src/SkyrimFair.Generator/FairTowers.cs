using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// The festival light towers described by <see cref="TowersConfig"/>: Barry's scaffold
/// watchtower, with no guard. A large lantern stands where the guard would: a scaled-up
/// vanilla candle lantern with its physics removed, a small fire inside it, a soft glow
/// round it and a vanilla Whiterun fire light. Tall Whiterun city banners hang from the
/// deck's edge on the faces named per tower, tilted back as Dragonsreach hangs them.
///
/// The tower's ladder is on its local -X face, so no banner hangs there. A banner hangs
/// from its origin, with the cloth across its local Y and swaying out toward its local -X,
/// so each one is turned with -X pointing away from the tower.
/// </summary>
internal static class FairTowers
{
    private const float Deg = MathF.PI / 180f;

    public static TowersResult Build(
        SkyrimMod mod, TowersConfig config, Static tower, Static lantern, Func<float, float, float> ground,
        Action<PlacedObject> put)
    {
        var banner = FormKeyHelper.Parse(config.Banner);
        var light = FormKeyHelper.Parse(config.Light);
        var fire = FormKeyHelper.Parse(config.Fire);
        var glow = FormKeyHelper.Parse(config.Glow);
        var scale = config.Tower.Scale;
        var footprints = new List<(float X, float Y)[]>();
        var banners = 0;

        for (var i = 0; i < config.Towers.Count; i++)
        {
            var spot = config.Towers[i];
            var (x, y) = (spot.X, spot.Y);
            var z = ground(x, y) - config.Sink;
            var yaw = spot.Yaw * Deg;

            // Local (lx, ly) to world, by the clockwise-positive Z rotation.
            (float X, float Y) World(float lx, float ly)
                => (x + lx * MathF.Cos(yaw) + ly * MathF.Sin(yaw), y - lx * MathF.Sin(yaw) + ly * MathF.Cos(yaw));

            put(new PlacedObject(mod)
            {
                EditorID = $"{config.EditorIdPrefix}{i + 1:00}",
                Base = new FormLinkNullable<IPlaceableObjectGetter>(tower.FormKey),
                Scale = scale,
                Placement = new Placement
                {
                    Position = new P3Float(x, y, z),
                    Rotation = new P3Float(0f, 0f, yaw),
                },
            });

            var deckZ = z + config.DeckZ * scale;
            var floorZ = z + config.FloorZ * scale;
            put(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(lantern.FormKey),
                Scale = config.LanternScale,
                Placement = new Placement
                {
                    Position = new P3Float(x, y, floorZ),
                    Rotation = new P3Float(0f, 0f, yaw),
                },
            });
            put(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(light),
                Placement = new Placement
                {
                    Position = new P3Float(x, y, floorZ + config.LightZ),
                    Rotation = new P3Float(0f, 0f, 0f),
                },
            });
            put(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(fire),
                Scale = config.FireScale,
                Placement = new Placement
                {
                    Position = new P3Float(x, y, floorZ + config.FireZ),
                    Rotation = new P3Float(0f, 0f, 0f),
                },
            });
            put(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(glow),
                Scale = config.GlowScale,
                Placement = new Placement
                {
                    Position = new P3Float(x, y, floorZ + config.GlowZ),
                    Rotation = new P3Float(0f, 0f, 0f),
                },
            });

            foreach (var face in spot.Banners)
            {
                var (nx, ny) = face switch
                {
                    "+X" => (1f, 0f),
                    "-X" => (-1f, 0f),
                    "+Y" => (0f, 1f),
                    "-Y" => (0f, -1f),
                    _ => throw new InvalidOperationException($"Tower {i + 1}: banner face '{face}' is not +X, -X, +Y or -Y."),
                };
                var reach = config.DeckHalf * scale + config.BannerOut;
                var (bx, by) = World(nx * reach, ny * reach);

                // World outward normal; the banner's local -X points along it.
                var (wx, wy) = (World(nx, ny).X - x, World(nx, ny).Y - y);
                var bannerYaw = MathF.Atan2(wy, -wx);  // local +X = (cos t, -sin t) = -n

                // Tilt back about the banner's local Y, split into the world-axis X and Y
                // rotations Skyrim applies after Z; Dragonsreach's (0,-23,0), (0,23,180),
                // (-23,0,90) and (23,0,270) are this at the four yaws.
                var tilt = config.BannerTiltDegrees * Deg;
                var (tiltX, tiltY) = (-tilt * MathF.Sin(bannerYaw), -tilt * MathF.Cos(bannerYaw));
                put(new PlacedObject(mod)
                {
                    Base = new FormLinkNullable<IPlaceableObjectGetter>(banner),
                    Scale = config.BannerScale,
                    Placement = new Placement
                    {
                        Position = new P3Float(bx, by, deckZ + config.BannerZ),
                        Rotation = new P3Float(tiltX, tiltY, bannerYaw),
                    },
                });
                banners++;
            }

            // The market keeps its stalls and dressing off the tower and its banners.
            var half = config.DeckHalf * scale + config.KeepOutMargin;
            footprints.Add(new[] { World(-half, -half), World(half, -half), World(half, half), World(-half, half) });
        }

        return new TowersResult(config.Towers.Count, banners, footprints);
    }
}

internal sealed record TowersResult(int Towers, int Banners, IReadOnlyList<(float X, float Y)[]> Footprints);
