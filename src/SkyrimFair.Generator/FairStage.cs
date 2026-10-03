using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SkyrimFair.Generator;

/// <summary>
/// Builds the main stage described by <see cref="StageConfig"/>: an open timber pavilion
/// of vanilla pieces, placed and turned by the configured stage zone's marker so it sits
/// on the avenue's sightline and faces the gate.
///
/// Pieces, chosen after an audit of the vanilla timber kits (see docs/AUDIT.md):
/// the farmhouse porch <c>Walkway01</c> for the deck, stockade scaffold plank tops for
/// broad shallow steps, stockade plank panels for the deck skirt, and one bark log,
/// <c>StockadeWoodbeam01</c>, for every post, beam, rafter and brace.
///
/// Rotation convention: Skyrim applies a reference's rotations about the world axes, Z
/// first, then Y, then X (the approved pitched stair cheeks depend on it). Every log here
/// is therefore laid with a yaw, then at most one tilt about a world axis at right angles
/// to it, which needs the stage to face along a world axis. Braces are crossing pairs
/// about a shared centre, so they come out the same whichever way a tilt's sign runs.
/// </summary>
internal static class FairStage
{
    private const float Deg = MathF.PI / 180f;

    public static StageResult Build(
        SkyrimMod mod, FairWorldConfig world, Func<float, float, float> ground, Action<PlacedObject> put)
    {
        var s = world.Stage;
        var zone = world.Zones.First(z => z.Name == s.Zone);
        var heading = zone.Marker[2];
        var h = heading * Deg;

        // The stage frame: F toward the audience, R across, both world axis-aligned.
        var f = (X: Round(MathF.Sin(h)), Y: Round(MathF.Cos(h)));
        var r = (X: Round(MathF.Cos(h)), Y: Round(-MathF.Sin(h)));
        var origin = (X: zone.Marker[0] + f.X * s.ForwardOffset, Y: zone.Marker[1] + f.Y * s.ForwardOffset);
        var baseZ = ground(origin.X, origin.Y);

        (float X, float Y) At(float u, float v) => (origin.X + u * r.X + v * f.X, origin.Y + u * r.Y + v * f.Y);

        var counts = new Dictionary<string, int>();
        var serial = 0;

        void Place(string role, string piece, float u, float v, float z, float yaw,
            float rotX = 0f, float rotY = 0f, float scale = 1f)
        {
            var (x, y) = At(u, v);
            put(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(piece)),
                Placement = new Placement
                {
                    Position = new P3Float(x, y, baseZ + z),
                    Rotation = new P3Float(rotX * Deg, rotY * Deg, yaw * Deg),
                },
                Scale = MathF.Abs(scale - 1f) < 1e-4f ? null : scale,
            });
            counts[role] = counts.GetValueOrDefault(role) + 1;
            serial++;
        }

        // Yaw that lays a piece's local Y along a stage direction.
        float YawAlong((float X, float Y) d) => MathF.Atan2(d.X, d.Y) / Deg;

        // ---- deck --------------------------------------------------------------
        var deck = s.Deck;
        var halfWidth = deck.Columns * deck.PieceWidth / 2f;
        var halfDepth = deck.Rows * deck.PieceDepth / 2f;
        for (var row = 0; row < deck.Rows; row++)
        {
            for (var col = 0; col < deck.Columns; col++)
            {
                var u = (col - (deck.Columns - 1) / 2f) * deck.PieceWidth;
                var v = (row - (deck.Rows - 1) / 2f) * deck.PieceDepth;
                Place("deck", deck.Piece, u - deck.PieceCentreX, v - deck.PieceCentreY, s.DeckHeight - deck.PieceTopZ, heading);
            }
        }

        // ---- steps: broad shallow treads, each resting on the one below ----------
        var steps = s.Steps;
        var rise = s.DeckHeight / (steps.Treads + 1);
        var stairHalf = steps.Across * steps.PieceWidth / 2f;
        for (var k = 1; k <= steps.Treads; k++)
        {
            var top = s.DeckHeight - rise * k;
            var front = halfDepth + steps.Exposed * k;
            for (var i = 0; i < steps.Across; i++)
            {
                var u = (i - (steps.Across - 1) / 2f) * steps.PieceWidth;
                Place("steps", steps.Piece, u, front - steps.PieceDepth / 2f, top - steps.PieceTopZ, heading);
            }

            // A board under the tread's front edge closes the riser; it runs down past
            // the tread below, where the tread hides it.
            foreach (var u in Fill(-stairHalf, stairHalf, steps.RiserLength, 0f))
            {
                Place("risers", steps.RiserPiece, u, front - 4f, top - steps.RiserTopZ, YawAlong(r));
            }
        }

        // ---- skirt boards round the deck, open where the steps meet it ------------
        var skirt = s.Skirt;
        if (skirt.Enabled)
        {
            var z = s.DeckHeight - skirt.BelowDeck - skirt.TopZ;
            var alongWidth = YawAlong(r);
            var alongDepth = YawAlong(f);
            foreach (var face in skirt.Faces)
            {
                switch (face)
                {
                    case "front":
                        foreach (var (a, b) in new[] { (-halfWidth, -stairHalf), (stairHalf, halfWidth) })
                            foreach (var c in Fill(a, b, skirt.Length, 10f))
                                Place("skirt", skirt.Piece, c, halfDepth + skirt.Gap, z, alongWidth);
                        break;
                    case "back":
                        foreach (var c in Fill(-halfWidth, halfWidth, skirt.Length, 10f))
                            Place("skirt", skirt.Piece, c, -halfDepth - skirt.Gap, z, alongWidth);
                        break;
                    case "left":
                    case "right":
                        var side = face == "left" ? -1f : 1f;
                        foreach (var c in Fill(-halfDepth, halfDepth, skirt.Length, 10f))
                            Place("skirt", skirt.Piece, side * (halfWidth + skirt.Gap), c, z, alongDepth);
                        break;
                }
            }
        }

        // ---- timber frame ---------------------------------------------------------
        var log = s.Log;

        // A log laid along a stage direction, then tilted up by `pitch` in the vertical
        // plane containing that direction (a tilt about the world axis across it).
        void Log(string role, float u, float v, float z, (float X, float Y) dir, float length,
            float pitch, float scaleJitter, float yawJitter)
        {
            var yaw = YawAlong(dir) + yawJitter;
            var alongWorldX = MathF.Abs(dir.X) > 0.5f;
            Place(role, log.Piece, u, v, z, yaw,
                rotX: alongWorldX ? 0f : pitch,
                rotY: alongWorldX ? pitch : 0f,
                scale: length / log.Length * (1f + scaleJitter));
        }

        // Posts: a log turned upright, with a slight lean and a little variation.
        for (var i = 0; i < s.Posts.Count; i++)
        {
            var (u, v) = (s.Posts[i][0], s.Posts[i][1]);
            var length = s.PostHeight * (1f + FairHash.Signed(i, 1, 31) * s.ScaleJitter);
            var sink = FairHash.Hash3(i, 1, 32) * 12f;
            var lean = FairHash.Signed(i, 1, 33) * s.PostLeanDegrees;
            var flip = FairHash.Hash3(i, 1, 34) < 0.5f ? 180f : 0f;
            // Along world Y (yaw 0 or 180) and stood up about world X; the lean is about Y.
            var (x, y) = At(u, v);
            put(new PlacedObject(mod)
            {
                Base = new FormLinkNullable<IPlaceableObjectGetter>(FormKeyHelper.Parse(log.Piece)),
                Placement = new Placement
                {
                    Position = new P3Float(x, y, baseZ + length / 2f - sink),
                    Rotation = new P3Float(90f * Deg, lean * Deg, flip * Deg),
                },
                Scale = length / log.Length,
            });
            counts["posts"] = counts.GetValueOrDefault("posts") + 1;
        }

        // Beams and rafters: runs of logs, overlapped, each wandering a little.
        void Run(string role, float[] from, float[] to, float z, float pieceScale, int salt)
        {
            var (du, dv) = (to[0] - from[0], to[1] - from[1]);
            var span = MathF.Sqrt(du * du + dv * dv);
            var dir = (X: (du * r.X + dv * f.X) / span, Y: (du * r.Y + dv * f.Y) / span);
            var pieceLength = log.Length * pieceScale;
            var k = 0;
            foreach (var along in Fill(0f, span, pieceLength, log.Overlap))
            {
                var t = along / span;
                Log(role, from[0] + du * t, from[1] + dv * t,
                    z + FairHash.Signed(salt, k, 41) * s.ZJitter, dir, pieceLength, 0f,
                    FairHash.Signed(salt, k, 42) * s.ScaleJitter,
                    FairHash.Signed(salt, k, 43) * s.YawJitterDegrees);
                k++;
            }
        }

        for (var i = 0; i < s.Beams.Count; i++)
        {
            var b = s.Beams[i];
            Run("beams", b.From, b.To, b.Z, log.BeamScale, 100 + i);
        }

        for (var i = 0; i < s.Rafters.U.Count; i++)
        {
            var u = s.Rafters.U[i];
            Run("rafters", new[] { u, s.Rafters.FromV }, new[] { u, s.Rafters.ToV }, s.Rafters.Z, s.Rafters.Scale, 200 + i);
        }

        // X-braced bays: two logs crossing at the bay centre.
        for (var i = 0; i < s.Braces.Count; i++)
        {
            var b = s.Braces[i];
            var (du, dv) = (b.To[0] - b.From[0], b.To[1] - b.From[1]);
            var width = MathF.Sqrt(du * du + dv * dv);
            var height = b.ZHigh - b.ZLow;
            var dir = (X: (du * r.X + dv * f.X) / width, Y: (du * r.Y + dv * f.Y) / width);
            var pitch = MathF.Atan2(height, width) / Deg;
            var length = MathF.Sqrt(width * width + height * height);
            var (cu, cv) = ((b.From[0] + b.To[0]) / 2f, (b.From[1] + b.To[1]) / 2f);
            foreach (var sign in new[] { 1f, -1f })
            {
                Log("braces", cu, cv, (b.ZLow + b.ZHigh) / 2f, dir, length, sign * pitch,
                    FairHash.Signed(300 + i, (int)sign, 51) * s.ScaleJitter * 0.5f, 0f);
            }
        }

        // ---- fire braziers on the deck ------------------------------------------------
        foreach (var b in s.Braziers)
        {
            var feet = s.DeckHeight + s.BrazierFeet;
            Place("braziers", s.BrazierPiece, b[0], b[1], feet, 0f);
            var (ox, oy) = (s.FireOffset[0], s.FireOffset[1]);
            // The fire's offset is in world units; convert it to the stage frame.
            Place("fire", s.FirePiece,
                b[0] + ox * r.X + oy * r.Y, b[1] + ox * f.X + oy * f.Y, feet + s.FireOffset[2], 0f);
        }

        var (cx, cy) = At(0f, 0f);
        var (fx, fy) = At(0f, halfDepth);
        var (sx, sy) = At(0f, halfDepth + steps.Exposed * steps.Treads);
        return new StageResult(
            cx, cy, baseZ, heading, halfWidth * 2f, halfDepth * 2f, s.DeckHeight,
            stairHalf * 2f, steps.Exposed * steps.Treads, rise,
            (fx, fy), (sx, sy), counts, serial + counts.GetValueOrDefault("posts"));
    }

    /// <summary>Centres of the fewest pieces of length <paramref name="piece"/> that cover a..b overlapping by at least <paramref name="overlap"/>.</summary>
    private static IEnumerable<float> Fill(float a, float b, float piece, float overlap)
    {
        var span = b - a;
        if (span <= piece)
        {
            yield return (a + b) / 2f;
            yield break;
        }

        var count = (int)MathF.Ceiling((span - piece) / (piece - overlap)) + 1;
        for (var k = 0; k < count; k++)
        {
            yield return a + piece / 2f + k * (span - piece) / (count - 1);
        }
    }

    private static float Round(float v) => MathF.Round(v * 1e5f) / 1e5f;
}

internal sealed record StageResult(
    float CentreX,
    float CentreY,
    float GroundZ,
    float Heading,
    float DeckWidth,
    float DeckDepth,
    float DeckHeight,
    float StairWidth,
    float StairRun,
    float Riser,
    (float X, float Y) DeckFront,
    (float X, float Y) StairFoot,
    IReadOnlyDictionary<string, int> Counts,
    int Total);
