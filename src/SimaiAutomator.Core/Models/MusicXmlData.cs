namespace SimaiAutomator.Core.Models;

public class MusicXmlData
{
    public string DataName { get; set; } = string.Empty;
    public int Id6 { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public int GenreId { get; set; } = 101;
    public string GenreName { get; set; } = "POPSアニメ";
    public decimal Bpm { get; set; }
    public int AddVersionId { get; set; } = 100;
    public string AddVersionName { get; set; } = "Custom";
    public NoteEntry[] NotesData { get; set; } = new NoteEntry[6];
    public string SortName { get; set; } = "A";
    public string CueId { get; set; } = string.Empty;
    public string CueStr { get; set; } = string.Empty;
    public string MovieId { get; set; } = string.Empty;
    public string MovieStr { get; set; } = string.Empty;
}

public class NoteEntry
{
    public string FilePath { get; set; } = string.Empty;
    public int Level { get; set; }
    public int LevelDecimal { get; set; }
    public int NotesDesignerId { get; set; } = 999;
    public string NotesDesignerStr { get; set; } = string.Empty;
    public int MusicLevelId { get; set; }
    public bool IsEnable { get; set; }
}
