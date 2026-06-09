using System.Diagnostics;
using SimaiAutomator.Core.Models;

namespace SimaiAutomator.Core.Services;

public class Pipeline
{
    private readonly string _toolsDir;
    private readonly string _outputBase;

    public Pipeline(string toolsDir, string outputBase)
    {
        _toolsDir = toolsDir;
        _outputBase = outputBase;
    }

    public async Task<string> RunAsync(
        string projectDir, int id6, string optName, Action<string> onProgress)
    {
        var isDx = (id6 / 10000) % 10 == 1;

        onProgress("解析 maidata.txt...");
        var maidataPath = Path.Combine(projectDir, "maidata.txt");
        if (!File.Exists(maidataPath))
            throw new FileNotFoundException($"找不到 maidata.txt: {maidataPath}");

        var project = MaidataParser.Parse(maidataPath);

        onProgress($"标题: {project.Title}");
        onProgress($"作者: {project.Artist}");
        onProgress($"BPM: {project.Bpm}");

        var validNumbers = project.ValidSimaiNumbers;
        for (int i = 0; i < validNumbers.Count; i++)
        {
            var simaiNum = validNumbers[i];
            var entry = project.Charts[simaiNum];
            var slotLabel = ChartDifficultyEx.Label((ChartDifficulty)i);
            onProgress($"  {slotLabel}: Lv.{entry.LevelDisplay} by {entry.Charter}");
        }

        if (project.FirstOffset > 0)
            onProgress($"偏移: {project.FirstOffset}s");

        onProgress("创建目录结构...");
        var outputDir = Path.Combine(_outputBase, optName);
        var musicDir = Path.Combine(outputDir, "music", $"music{id6:D6}");
        var soundDir = Path.Combine(outputDir, "SoundData");
        var jacketDir = Path.Combine(outputDir, "LocalAssets");
        var movieDir = Path.Combine(outputDir, "MovieData");

        Directory.CreateDirectory(musicDir);
        Directory.CreateDirectory(soundDir);
        Directory.CreateDirectory(jacketDir);

        // Convert charts in order: simai numbers sorted, map to slots 0-4
        for (int slot = 0; slot < validNumbers.Count; slot++)
        {
            var simaiNum = validNumbers[slot];
            var diff = (ChartDifficulty)slot; // slot = output position
            onProgress($"转换谱面 {diff.Label()} (simai {simaiNum})...");

            var tempOut = Path.Combine(Path.GetTempPath(), $"simai_conv_{Guid.NewGuid():N}");
            var ma2Path = await ChartConverter.ConvertAsync(
                maidataPath, diff, simaiNum, tempOut, _toolsDir, isDx);

            var dest = Path.Combine(musicDir, $"{id6:D6}_{diff.ToFileSuffix()}.ma2");
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(ma2Path, dest);
        }

        onProgress("生成 Music.xml...");
        var xmlData = XmlGenerator.Build(project, id6);
        File.WriteAllText(Path.Combine(musicDir, "Music.xml"), XmlGenerator.GenerateMusicXml(xmlData));

        if (project.AudioPath != null)
        {
            var templateAcb = FindTemplateAcb();
            if (templateAcb != null && AceAutomator.IsAvailable(_toolsDir))
            {
                onProgress("转换音频...");
                var outAcb = Path.Combine(soundDir, $"music{id6:D6}.acb");
                var outAwb = Path.Combine(soundDir, $"music{id6:D6}.awb");

                var ok = await AceAutomator.ReplaceAudioAsync(
                    _toolsDir, templateAcb, project.AudioPath, outAcb, outAwb);
                if (!ok) onProgress("音频: 请在 ACE 窗口中完成替换后按 Enter");
            }
        }

        if (project.CoverPath != null) { onProgress("复制封面..."); CoverHandler.CopyToLocalAssets(project.CoverPath, jacketDir, id6); }
        if (project.BgaPath != null) { onProgress("复制 BGA..."); CoverHandler.CopyBga(project.BgaPath, movieDir, id6); }

        onProgress($"完成! 输出: {outputDir}");
        return outputDir;
    }

    private string? FindTemplateAcb()
    {
        foreach (var p in new[] { Path.Combine(_toolsDir, "template.acb"), Path.Combine(_toolsDir, "..", "template.acb") })
            if (File.Exists(p)) return p;
        return null;
    }
}
