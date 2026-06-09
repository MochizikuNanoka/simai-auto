namespace SimaiAutomator.Core.Models;

/// <summary>Parsed simai project.</summary>
public class SimaiProject
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Charter { get; set; } = string.Empty;
    public decimal Bpm { get; set; }
    public decimal FirstOffset { get; set; }

    public Dictionary<ChartDifficulty, ChartEntry> Charts { get; set; } = new();

    public string? AudioPath { get; set; }
    public string? CoverPath { get; set; }
    public string? BgaPath { get; set; }
    public string? ProjectDir { get; set; }
}

public class ChartEntry
{
    public string LevelDisplay { get; set; } = "0";  // raw "12+" or "木镐"
    public int LevelNumber { get; set; }              // parsed int, 0 for custom
    public int LevelDecimal { get; set; }              // parsed decimal part
    public string Charter { get; set; } = string.Empty;
    public string ChartBody { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}
