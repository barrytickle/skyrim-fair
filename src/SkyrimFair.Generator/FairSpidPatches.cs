using System.Text;

namespace SkyrimFair.Generator;

/// <summary>
/// SPID exclusion patches: copies of other mods' <c>_DISTR.ini</c> files with
/// <c>-SkyrimFairNPC</c> added to the lines that give every NPC a cloak or a per-effect
/// script. They carry the same filename, so installed above the mod (MO2 priority), the
/// copy replaces its file and the mod's own folder is never edited.
///
/// Each is regenerated from the mod's current file on every build, so after a mod update
/// one rebuild carries the change over. If a named line is missing, or no longer has the
/// expected shape, the build fails rather than shipping a stale copy.
///
/// Why this and not an ESP patch: a Cloak keeps casting whatever its ability's
/// conditions say, and templated fair NPCs are runtime copies that a plugin filter misses;
/// the keyword survives on them (docs/RELEASE.md).
/// </summary>
internal static class FairSpidPatches
{
    public static List<(string File, int Lines)> Build(FairConfig config, string outputDirectory)
    {
        var built = new List<(string, int)>();
        if (config.SpidPatches.Count == 0)
        {
            return built;
        }

        var outDir = Path.Combine(outputDirectory, "spid");
        if (Directory.Exists(outDir))
        {
            Directory.Delete(outDir, recursive: true);
        }

        foreach (var patch in config.SpidPatches)
        {
            var source = Path.IsPathRooted(patch.Source) ? patch.Source : Path.Combine(FairPaths.ConfigDirectory, patch.Source);
            if (!File.Exists(source))
            {
                Console.WriteLine($"  SPID patch {Path.GetFileName(source)}: skipped, {patch.Source} not found");
                continue;
            }

            var bytes = File.ReadAllBytes(source);
            var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var text = Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
            var newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Split(newline);
            var changed = 0;
            foreach (var form in patch.Spells)
            {
                var found = false;
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var body = line.TrimStart();
                    if (!body.StartsWith("Spell", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var eq = body.IndexOf('=');
                    if (eq < 0)
                    {
                        continue;
                    }

                    var fields = body[(eq + 1)..].Trim().Split('|');
                    if (!fields[0].Trim().Equals(form, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (fields.Length < 2)
                    {
                        throw new InvalidOperationException($"SPID patch {Path.GetFileName(source)}: the {form} line has no string-filter field; check the mod's new format");
                    }

                    // The string-filter field: NONE becomes the exclusion alone; otherwise it's added.
                    var filters = fields[1].Trim();
                    var exclusion = "-" + config.FairWorld.NpcKeyword;
                    if (!filters.Split(',').Any(f => f.Trim().Equals(exclusion, StringComparison.OrdinalIgnoreCase)))
                    {
                        fields[1] = filters.Equals("NONE", StringComparison.OrdinalIgnoreCase) ? exclusion : filters + "," + exclusion;
                    }

                    lines[i] = line[..(line.Length - body.Length)] + body[..(eq + 1)] + " " + string.Join("|", fields);
                    found = true;
                    changed++;
                }

                if (!found)
                {
                    throw new InvalidOperationException($"SPID patch {Path.GetFileName(source)}: no \"Spell = {form}\" line; the mod may have changed, check it and update spidPatches");
                }
            }

            Directory.CreateDirectory(outDir);
            var outBytes = Encoding.UTF8.GetBytes(string.Join(newline, lines));
            using (var fs = File.Create(Path.Combine(outDir, Path.GetFileName(source))))
            {
                if (bom)
                {
                    fs.Write(new byte[] { 0xEF, 0xBB, 0xBF });
                }

                fs.Write(outBytes);
            }

            built.Add((Path.GetFileName(source), changed));
        }

        return built;
    }
}
