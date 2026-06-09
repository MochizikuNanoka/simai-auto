using System.Diagnostics;
using SimaiAutomator.Core.Models;

namespace SimaiAutomator.Core.Services;

public static class ChartConverter
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(60);

    public static async Task<string> ConvertAsync(
        string maidataPath, ChartDifficulty slot, int simaiDifficulty,
        string outputDir, string toolsDir, bool isDx)
    {
        var exe = FindMaichartConverter(toolsDir);
        if (exe == null)
            throw new FileNotFoundException("找不到 MaichartConverter.exe，请放在 tools/ 目录下");

        Directory.CreateDirectory(outputDir);

        var safeDir = Path.Combine(Path.GetTempPath(), $"simai_chart_{Guid.NewGuid():N}");
        Directory.CreateDirectory(safeDir);

        var safeMaidata = Path.Combine(safeDir, "maidata.txt");
        File.Copy(maidataPath, safeMaidata, overwrite: true);

        var srcDir = Path.GetDirectoryName(maidataPath)!;
        foreach (var pattern in new[] { "track.*", "bg.*", "cover.*", "pv.*", "movie.*" })
        {
            var files = Directory.GetFiles(srcDir, pattern);
            foreach (var f in files)
            {
                var dest = Path.Combine(safeDir, Path.GetFileName(f));
                if (!File.Exists(dest))
                    File.Copy(f, dest, overwrite: true);
            }
        }

        var format = isDx ? "Ma2_104" : "Ma2_101";
        var args = $"compilesimai -p maidata.txt -f {format} -d {simaiDifficulty} -o \"{outputDir}\"";

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = safeDir
        };

        using var proc = Process.Start(psi);
        if (proc == null) throw new Exception("无法启动 MaichartConverter");

        string stdout = "", stderr = "";

        try
        {
            using var cts = new CancellationTokenSource(ProcessTimeout);
            var readOut = proc.StandardOutput.ReadToEndAsync(cts.Token);
            var readErr = proc.StandardError.ReadToEndAsync(cts.Token);

            await proc.WaitForExitAsync(cts.Token);
            stdout = await readOut;
            stderr = await readErr;
        }
        catch (OperationCanceledException)
        {
            KillProcessTree(proc);
            throw new Exception($"MaichartConverter 超时 ({ProcessTimeout.TotalSeconds}s) 已强制终止");
        }
        catch (Exception)
        {
            KillProcessTree(proc);
            throw;
        }
        finally
        {
            // Ensure process is dead
            if (!proc.HasExited)
                KillProcessTree(proc);

            try { Directory.Delete(safeDir, true); } catch { }
        }

        if (proc.ExitCode != 0)
        {
            var errorInfo = string.IsNullOrEmpty(stderr) ? stdout : stderr;
            throw new Exception($"MaichartConverter 失败 (代码 {proc.ExitCode}):\n{errorInfo.Trim()}");
        }

        var ma2Files = Directory.GetFiles(outputDir, "*.ma2");
        if (ma2Files.Length == 0)
            throw new Exception("MaichartConverter 未生成 ma2 文件");

        return ma2Files[0];
    }

    /// <summary>Kill the process and all its children to prevent lingering.</summary>
    private static void KillProcessTree(Process proc)
    {
        try
        {
            // Kill child processes first
            var childProc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/T /F /PID {proc.Id}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            childProc.Start();
            childProc.WaitForExit(3000);
        }
        catch { }

        try { proc.Kill(); } catch { }
    }

    private static string? FindMaichartConverter(string toolsDir)
    {
        var paths = new[]
        {
            Path.Combine(toolsDir, "MaichartConverter.exe"),
            Path.Combine(AppContext.BaseDirectory, "tools", "MaichartConverter.exe"),
            Path.Combine(AppContext.BaseDirectory, "MaichartConverter.exe"),
        };
        return paths.FirstOrDefault(File.Exists);
    }
}
