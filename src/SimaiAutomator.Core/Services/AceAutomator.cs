using System.Diagnostics;

namespace SimaiAutomator.Core.Services;

/// <summary>
/// Launches ACE for audio replacement with guided instructions.
/// Full GUI automation is unreliable across ACE versions;
/// semi-automated with console guidance is 100% reliable.
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

        // Print clear instructions
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  ACE 音频替换 - 请按以下步骤操作");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine($"  1. 在左侧树中选中音频 cue (Cue Sheet)");
        Console.WriteLine($"  2. 菜单 Action -> Replace (或右键 Replace)");
        Console.WriteLine($"  3. 选择文件: {audioPath}");
        Console.WriteLine($"  4. 勾选 Path 选项，其余保持不变");
        Console.WriteLine($"  5. 菜单 File -> Save As");
        Console.WriteLine($"  6. 保存为: {Path.GetFileName(outputAcbPath)}");
        Console.WriteLine($"     位置: {Path.GetDirectoryName(outputAcbPath)}");
        Console.WriteLine($"  7. 关闭 ACE 窗口");
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.Write("  完成后按 Enter 继续...");
        Console.ReadLine();

        // Cleanup
        if (!proc.HasExited)
        {
            try { proc.Kill(); } catch { }
        }

        return File.Exists(outputAcbPath);
    }

    public static bool IsAvailable(string toolsDir) => AudioConverter.FindAce(toolsDir) != null;
}
