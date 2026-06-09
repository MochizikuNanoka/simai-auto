using System.Text.RegularExpressions;
using SimaiAutomator.Core.Models;

namespace SimaiAutomator.Core.Services;

public static class MaidataParser
{
    public static SimaiProject Parse(string maidataPath)
    {
        var dir = Path.GetDirectoryName(maidataPath) ?? ".";
        var project = new SimaiProject { ProjectDir = dir };

        if (!File.Exists(maidataPath))
            throw new FileNotFoundException($"找不到 maidata.txt: {maidataPath}");

        // Read with BOM handling
        var text = File.ReadAllText(maidataPath);
        text = text.TrimStart('\uFEFF'); // strip BOM
        var lines = text.Split('\n');

        // Scan companion files
        project.AudioPath = GlobFirst(dir, "track.*");
        project.CoverPath = GlobFirst(dir, "bg.*") ?? GlobFirst(dir, "cover.*");
        project.BgaPath = GlobFirst(dir, "pv.*") ?? GlobFirst(dir, "movie.*");

        // Temporary storage for chart bodies being built
        int currentInote = -1;
        decimal currentBpm = 0;
        var chartLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim('\r', '\n').Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith('&'))
            {
                // Flush previous chart body
                if (currentInote > 0 && chartLines.Count > 0)
                {
                    FlushChart(project, currentInote, currentBpm, chartLines);
                    chartLines.Clear();
                }

                ParseDirective(project, line, ref currentInote, ref currentBpm, chartLines);
            }
            else if (currentInote > 0)
            {
                chartLines.Add(line);
            }
        }

        // Flush last chart
        if (currentInote > 0 && chartLines.Count > 0)
            FlushChart(project, currentInote, currentBpm, chartLines);

        // Inherit BPM from charts if not set globally
        if (project.Bpm == 0)
        {
            foreach (var (_, entry) in project.Charts.Where(c => c.Value.IsValid))
            {
                if (entry.ChartBody.Contains('(') && entry.ChartBody.Contains(')'))
                {
                    var bpmMatch = Regex.Match(entry.ChartBody, @"\((\d+\.?\d*)\)");
                    if (bpmMatch.Success && decimal.TryParse(bpmMatch.Groups[1].Value, out var b))
                    {
                        project.Bpm = b;
                        break;
                    }
                }
            }
        }

        return project;
    }

    private static void ParseDirective(SimaiProject p, string line,
        ref int inote, ref decimal bpm, List<string> chartLines)
    {
        if (Match(line, "&title=", out var v)) p.Title = v;
        else if (Match(line, "&artist=", out v)) p.Artist = v;
        else if (Match(line, "&des=", out v)) p.Charter = v;
        else if (Match(line, "&first=", out v) && decimal.TryParse(v, out var f)) p.FirstOffset = f;
        else if (Match(line, "&bpm=", out v) && decimal.TryParse(v, out var bp)) p.Bpm = bp;
        else if (TryInote(line, out var num, out var bp2, out var rest))
        {
            inote = num;
            if (bp2.HasValue) bpm = bp2.Value;
            if (!string.IsNullOrEmpty(rest)) chartLines.Add(rest);
        }
        else if (TryLvDes(line, out var diff, out var lv, out var des))
        {
            if (!p.Charts.ContainsKey(diff))
                p.Charts[diff] = new ChartEntry();
            if (lv != null) p.Charts[diff].LevelDisplay = lv;
            if (des != null) p.Charts[diff].Charter = des;
        }
    }

    private static bool Match(string line, string prefix, out string value)
    {
        value = string.Empty;
        var idx = line.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        value = line[(idx + prefix.Length)..].Trim();
        return true;
    }

    private static bool TryInote(string line, out int difficulty, out decimal? bpm, out string rest)
    {
        difficulty = 0; bpm = null; rest = string.Empty;
        var m = Regex.Match(line, @"^&inote_(\d+)\s*=\s*(?:\((\d+\.?\d*)\))?(.*)$", RegexOptions.IgnoreCase);
        if (!m.Success) return false;
        difficulty = int.Parse(m.Groups[1].Value);
        if (m.Groups[2].Success && decimal.TryParse(m.Groups[2].Value, out var bp))
            bpm = bp;
        rest = m.Groups[3].Value.Trim();
        return true;
    }

    private static bool TryLvDes(string line, out ChartDifficulty diff, out string? lv, out string? des)
    {
        diff = ChartDifficulty.Reserved; lv = null; des = null;

        var mLv = Regex.Match(line, @"^&lv_(\d+)\s*=\s*(.*)$", RegexOptions.IgnoreCase);
        var mDes = Regex.Match(line, @"^&des_(\d+)\s*=\s*(.*)$", RegexOptions.IgnoreCase);

        if (mLv.Success)
        {
            diff = ChartDifficultyEx.FromSimaiNumber(int.Parse(mLv.Groups[1].Value));
            lv = mLv.Groups[2].Value.Trim();
        }
        if (mDes.Success)
        {
            var d = ChartDifficultyEx.FromSimaiNumber(int.Parse(mDes.Groups[1].Value));
            if (diff == ChartDifficulty.Reserved) diff = d;
            des = mDes.Groups[2].Value.Trim();
        }

        return mLv.Success || mDes.Success;
    }

    private static void FlushChart(SimaiProject p, int simaiNum, decimal bpm, List<string> lines)
    {
        var diff = ChartDifficultyEx.FromSimaiNumber(simaiNum);
        var body = string.Join("\n", lines).Trim();

        // Discard placeholder charts
        var stripped = Regex.Replace(body, @"\([^)]*\)", "");  // remove (BPM) directives
        stripped = Regex.Replace(stripped, @"\{[^}]*\}", "");   // remove {N} directives
        stripped = stripped.Replace(",", "").Replace("\n", "").Replace(" ", "").Trim();
        var isValid = !string.IsNullOrEmpty(body)
            && stripped != "E"
            && stripped.Length > 1
            && body.Contains(',');

        if (!p.Charts.ContainsKey(diff))
            p.Charts[diff] = new ChartEntry();

        p.Charts[diff].ChartBody = body;
        p.Charts[diff].IsValid = isValid;

        // Parse level display
        if (string.IsNullOrEmpty(p.Charts[diff].LevelDisplay) || p.Charts[diff].LevelDisplay == "0")
        {
            ParseLevel(p.Charts[diff].LevelDisplay ?? "0", out var num, out var dec);
            p.Charts[diff].LevelNumber = num;
            p.Charts[diff].LevelDecimal = dec;
        }

        // Inherit global charter if per-difficulty not set
        if (string.IsNullOrEmpty(p.Charts[diff].Charter) && !string.IsNullOrEmpty(p.Charter))
            p.Charts[diff].Charter = p.Charter;
    }

    public static void ParseLevel(string display, out int level, out int decimalPart)
    {
        level = 0; decimalPart = 0;
        if (string.IsNullOrEmpty(display)) return;

        // Try "12.7" format first
        if (decimal.TryParse(display, out var d))
        {
            level = (int)Math.Floor(d);
            decimalPart = (int)((d - level) * 10);
            return;
        }

        // Try "12+" format
        var plus = Regex.Match(display, @"^(\d+)\+$");
        if (plus.Success)
        {
            level = int.Parse(plus.Groups[1].Value);
            decimalPart = 0;
            return;
        }

        // Fallback: pure string like "木镐" → 0
        level = 0;
        decimalPart = 0;
    }

    private static string? GlobFirst(string dir, string pattern)
    {
        try { return Directory.GetFiles(dir, pattern).FirstOrDefault(); }
        catch { return null; }
    }
}
