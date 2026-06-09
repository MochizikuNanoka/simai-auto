using System.Diagnostics;

namespace SimaiAutomator.Core.Services;

/// <summary>
/// Launches ACE for audio replacement. Waits for the user to close ACE,
/// then continues automatically. No keyboard input needed.
/// </summary>
public static class AceAutomator
{
    public static async Task<bool> ReplaceAudioAsync(
        string toolsDir, string templateAcbPath, string audioPath,
        string outputAcbPath, string outputAwbPath)
    {
        var aceExe = AudioConverter.FindAce(toolsDir);
        if (aceExe == null) return false;

        // Copy template AWB
        var templateAwb = Path.ChangeExtension(templateAcbPath, ".awb");
        if (File.Exists(templateAwb))
            File.Copy(templateAwb, outputAwbPath, true);

        // Launch ACE
        var psi = new ProcessStartInfo
        {
            FileName = aceExe,
            Arguments = $"\"{templateAcbPath}\"",
            UseShellExecute = true
        };

        using var proc = Process.Start(psi);
        if (proc == null) return false;

        // Print instructions
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  ACE 音频替换");
        Console.WriteLine("========================================");
        Console.WriteLine($"  1. 左侧选中 Cue Sheet 下的音频 cue");
        Console.WriteLine($"  2. 右键 -> Replace (或菜单 Action -> Replace)");
        Console.WriteLine($"  3. 选择: {Path.GetFileName(audioPath)}");
        Console.WriteLine($"  4. 勾选 Path，其余不变，点 OK");
        Console.WriteLine($"  5. 菜单 File -> Save As");
        Console.WriteLine($"  6. 保存为: {Path.GetFileName(outputAcbPath)}");
        Console.WriteLine($"     到: {Path.GetDirectoryName(outputAcbPath)}");
        Console.WriteLine($"  7. 关闭 ACE 窗口继续...");
        Console.WriteLine("========================================");

        // Wait for ACE to close — user closes it when done
        await proc.WaitForExitAsync();

        return File.Exists(outputAcbPath);
    }

    public static bool IsAvailable(string toolsDir) => AudioConverter.FindAce(toolsDir) != null;
}
