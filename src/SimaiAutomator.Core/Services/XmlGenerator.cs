using System.Xml.Linq;
using System.Text;
using SimaiAutomator.Core.Models;

namespace SimaiAutomator.Core.Services;

public static class XmlGenerator
{
    public static string GenerateMusicXml(MusicXmlData d)
    {
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
        doc.Add(new XElement("MusicData",
            new XElement("dataName", d.DataName),
            new XElement("name", new XElement("id", d.Id6 % 10000), new XElement("str", d.Title)),
            new XElement("artistName", new XElement("id", d.Id6 % 10000), new XElement("str", d.Artist)),
            new XElement("genreName", new XElement("id", d.GenreId), new XElement("str", d.GenreName)),
            new XElement("bpm", d.Bpm),
            new XElement("AddVersion", new XElement("id", d.AddVersionId), new XElement("str", d.AddVersionName)),
            new XElement("notesData",
                d.NotesData.Select(n => new XElement("Notes",
                    new XElement("file", new XElement("path", n.FilePath)),
                    new XElement("level", n.Level),
                    new XElement("levelDecimal", n.LevelDecimal),
                    new XElement("notesDesigner", new XElement("id", n.NotesDesignerId), new XElement("str", n.NotesDesignerStr)),
                    new XElement("notesType", 0),
                    new XElement("musicLevelID", n.MusicLevelId),
                    new XElement("maxNotes", 0),
                    new XElement("isEnable", n.IsEnable ? "true" : "false")
                ))
            ),
            new XElement("movieName", new XElement("id", d.MovieId), new XElement("str", d.MovieStr)),
            new XElement("cueName", new XElement("id", d.CueId), new XElement("str", d.CueStr)),
            new XElement("sortName", d.SortName),
            new XElement("eventName", new XElement("id", 1), new XElement("str", "無期限常時解放")),
            new XElement("eventName2", new XElement("id", 0), new XElement("str", "解放なし")),
            new XElement("subEventName", new XElement("id", 0), new XElement("str", "解放なし")),
            new XElement("lockType", 0),
            new XElement("subLockType", 0)
        ));

        using var sw = new Utf8StringWriter();
        doc.Save(sw, SaveOptions.None);
        return sw.ToString();
    }

    public static string GenerateVersionXml(int verId, string verName)
    {
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
        doc.Add(new XElement("MusicVersionData",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
            new XElement("dataName", $"MusicVersion{verId:D6}"),
            new XElement("name", new XElement("id", verId), new XElement("str", verName)),
            new XElement("genreName", verName),
            new XElement("genreNameTwoLine", verName),
            new XElement("version", 50000),
            new XElement("Color", new XElement("R", 110), new XElement("G", 217), new XElement("B", 67)),
            new XElement("FileName", $"UI_CMN_TabTitle_MaimaiTitle_{verName}"),
            new XElement("priority", 0),
            new XElement("disable", "false")
        ));

        using var sw = new Utf8StringWriter();
        doc.Save(sw, SaveOptions.None);
        return sw.ToString();
    }

    public static MusicXmlData Build(SimaiProject p, int id6)
    {
        var d = new MusicXmlData
        {
            DataName = $"music{id6:D6}",
            Id6 = id6,
            Title = p.Title,
            Artist = p.Artist,
            Bpm = p.Bpm,
            CueId = (id6 % 10000).ToString(),
            CueStr = p.Title,
            MovieId = (id6 % 10000).ToString(),
            MovieStr = p.Title
        };

        for (int i = 0; i < 6; i++)
        {
            var diff = (ChartDifficulty)i;
            var has = p.Charts.TryGetValue(diff, out var entry) && entry.IsValid;
            d.NotesData[i] = new NoteEntry
            {
                FilePath = $"{id6:D6}_{i:D2}.ma2",
                Level = has ? entry!.LevelNumber : 0,
                LevelDecimal = has ? entry!.LevelDecimal : 0,
                NotesDesignerStr = has ? entry!.Charter : "",
                MusicLevelId = has ? ChartDifficultyEx.ToMusicLevelId(entry!.LevelNumber) : 0,
                IsEnable = has
            };
        }

        return d;
    }
}

internal class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}
