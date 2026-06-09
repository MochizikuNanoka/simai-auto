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
        string projectDir, int id6, string optName,
        Action<string> onProgress)
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
        foreach (var (diff, entry) in project.Charts.Where(c => c.Value.IsValid))
            onProgress($"  {diff.Label()}: Lv.{entry.LevelDisplay} by {entry.Charter}");

        if (project.FirstOffset > 0)
            onProgress($"偏移: {project.FirstOffset}s - 将自动裁剪音频");

        onProgress("创建目录结构...");
        var outputDir = Path.Combine(_outputBase, optName);
        var musicDir = Path.Combine(outputDir, "music", $"music{id6:D6}");
        var soundDir = Path.Combine(outputDir, "SoundData");
        var jacketDir = Path.Combine(outputDir, "LocalAssets");
        var movieDir = Path.Combine(outputDir, "MovieData");

        Directory.CreateDirectory(musicDir);
        Directory.CreateDirectory(soundDir);
        Directory.CreateDirectory(jacketDir);

        // Convert charts
        foreach (var (diff, _) in project.Charts.Where(c => c.Value.IsValid))
        {
            onProgress($"转换谱面 {diff.Label()}...");
            var tempOut = Path.Combine(Path.GetTempPath(), $"simai_conv_{diff.ToFileSuffix()}");
            var ma2Path = await ChartConverter.ConvertAsync(
                maidataPath, diff, diff.ToSimaiNumber(), tempOut, _toolsDir, isDx);
            var dest = Path.Combine(musicDir, $"{id6:D6}_{diff.ToFileSuffix()}.ma2");
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(ma2Path, dest);
        }

        // Generate XML
        onProgress("生成 Music.xml...");
        var xmlData = XmlGenerator.Build(project, id6);
        File.WriteAllText(Path.Combine(musicDir, "Music.xml"), XmlGenerator.GenerateMusicXml(xmlData));

        // Audio: auto via ACE or manual
        if (project.AudioPath != null)
        {
            var templateAcb = FindTemplateAcb();
            if (templateAcb != null && AceAutomator.IsAvailable(_toolsDir))
            {
                onProgress("转换音频 (ACE 自动化)...");
                var outAcb = Path.Combine(soundDir, $"music{id6:D6}.acb");
                var outAwb = Path.Combine(soundDir, $"music{id6:D6}.awb");

                // Handle first offset: trim audio if needed
                var audioToUse = project.AudioPath;
                if (project.FirstOffset > 0)
                {
                    audioToUse = await TrimAudioAsync(project.AudioPath, project.FirstOffset);
                    if (audioToUse != null)
                        onProgress($"音频已裁剪 {project.FirstOffset}s");
                }

                var ok = await AceAutomator.ReplaceAudioAsync(
                    _toolsDir, templateAcb, audioToUse!, outAcb, outAwb);

                if (ok)
                    onProgress("音频转换完成!");
                else
                    onProgress("ACE 自动化失败，请手动操作");
            }
            else
            {
                onProgress("未找到模板 ACB，跳过音频");
                onProgress($"请手动将音频放入: {soundDir}");
            }
        }

        // Cover
        if (project.CoverPath != null)
        {
            onProgress("复制封面...");
            CoverHandler.CopyToLocalAssets(project.CoverPath, jacketDir, id6);
        }

        // BGA
        if (project.BgaPath != null)
        {
            onProgress("复制 BGA (原始格式)...");
            CoverHandler.CopyBga(project.BgaPath, movieDir, id6);
        }

        onProgress($"完成! 输出: {outputDir}");
        return outputDir;
    }

    private string? FindTemplateAcb()
    {
        var paths = new[]
        {
            Path.Combine(_toolsDir, "template.acb"),
            Path.Combine(_toolsDir, "template.awb"),
            Path.Combine(_toolsDir, "..", "template.acb"),
        };
        foreach (var p in paths)
        {
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private static async Task<string?> TrimAudioAsync(string audioPath, decimal offsetSeconds)
    {
        // Try ffmpeg first
        var ffmpeg = FindFfmpeg();
        if (ffmpeg == null) return audioPath; // can't trim, use as-is

        var output = Path.Combine(Path.GetTempPath(), $"simai_trimmed_{Guid.NewGuid():N}{Path.GetExtension(audioPath)}");

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-y -ss {offsetSeconds} -i \"{audioPath}\" -acodec copy \"{output}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        if (proc == null) return audioPath;

        await proc.WaitForExitAsync();
        return proc.ExitCode == 0 ? output : audioPath;
    }

    private static string? FindFfmpeg()
    {
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe"),
            Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
            "ffmpeg",
            "ffmpeg.exe",
        };
        foreach (var p in paths)
        {
            try
            {
                var psi = new ProcessStartInfo { FileName = p, Arguments = "-version", UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(1000);
                if (proc?.ExitCode == 0) return p;
            }
            catch { }
        }
        return null;
    }
}
