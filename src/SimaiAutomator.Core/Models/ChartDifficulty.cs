namespace SimaiAutomator.Core.Models;

public enum ChartDifficulty
{
    Basic = 0, Advanced = 1, Expert = 2, Master = 3, ReMaster = 4, Reserved = 5
}

public static class ChartDifficultyEx
{
    public static string Label(this ChartDifficulty d) => d switch
    {
        ChartDifficulty.Basic => "绿 Basic",
        ChartDifficulty.Advanced => "黄 Advanced",
        ChartDifficulty.Expert => "红 Expert",
        ChartDifficulty.Master => "紫 Master",
        ChartDifficulty.ReMaster => "白 Re:Master",
        _ => "-"
    };

    public static string ToFileSuffix(this ChartDifficulty d) => d switch
    {
        ChartDifficulty.Basic => "00",
        ChartDifficulty.Advanced => "01",
        ChartDifficulty.Expert => "02",
        ChartDifficulty.Master => "03",
        ChartDifficulty.ReMaster => "04",
        _ => "05"
    };

    public static int ToSimaiNumber(this ChartDifficulty d) => d switch
    {
        ChartDifficulty.Basic => 1, ChartDifficulty.Advanced => 2,
        ChartDifficulty.Expert => 3, ChartDifficulty.Master => 4,
        ChartDifficulty.ReMaster => 5, _ => 0
    };

    public static ChartDifficulty FromSimaiNumber(int n) => n switch
    {
        1 => ChartDifficulty.Basic, 2 => ChartDifficulty.Advanced,
        3 => ChartDifficulty.Expert, 4 => ChartDifficulty.Master,
        5 => ChartDifficulty.ReMaster, _ => ChartDifficulty.Reserved
    };

    public static int ToMusicLevelId(int level)
    {
        return level switch
        {
            <= 7 => level, 8 => 9, 9 => 11, 10 => 13, 11 => 15,
            12 => 17, 13 => 19, 14 => 21, 15 => 23, _ => 0
        };
    }
}
