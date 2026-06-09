namespace SimaiAutomator.Core.Models;

public class SimaiProject
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Charter { get; set; } = string.Empty;
    public decimal Bpm { get; set; }
    public decimal FirstOffset { get; set; }

    /// <summary>Key = simai difficulty number (1-5), Value = chart data</summary>
    public Dictionary<int, ChartEntry> Charts { get; set; } = new();

    /// <summary>Valid charts sorted by simai number ascending</summary>
    public List<int> ValidSimaiNumbers => Charts
        .Where(c => c.Value.IsValid)
        .OrderBy(c => c.Key)
        .Select(c => c.Key)
        .ToList();

    public string? AudioPath { get; set; }
    public string? CoverPath { get; set; }
    public string? BgaPath { get; set; }
    public string? ProjectDir { get; set; }
}

public class ChartEntry
{
    public string LevelDisplay { get; set; } = "0";
    public int LevelNumber { get; set; }
    public int LevelDecimal { get; set; }
    public string Charter { get; set; } = string.Empty;
    public string ChartBody { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}
