using System.Diagnostics;

namespace SimaiAutomator.Core.Services;

/// <summary>
/// Handles audio conversion (mp3/ogg → ACB/AWB) via bundled ACE.
/// Since ACE is a GUI tool, this opens it and instructs the user.
/// </summary>
public static class AudioConverter
{
    /// <summary>
    /// Opens ACE for manual audio replacement.
    /// The user needs to: File→Open acb → Replace audio → Save.
    /// Returns the path to the template ACB file to start from.
    /// </summary>
    public static async Task<bool> OpenAceAsync(string toolsDir)
    {
        var aceExe = FindAce(toolsDir);
        if (aceExe == null)
            throw new FileNotFoundException("找不到 ACE.exe，请放在 tools/ 目录下");

        var psi = new ProcessStartInfo
        {
            FileName = aceExe,
            UseShellExecute = true,
            CreateNoWindow = false
        };

        using var proc = Process.Start(psi);
        if (proc == null) return false;

        await proc.WaitForExitAsync();
        return true;
    }

    /// <summary>
    /// Locates the ACE executable.
    /// </summary>
    public static string? FindAce(string toolsDir)
    {
        var paths = new[]
        {
            Path.Combine(toolsDir, "ACE.exe"),
            Path.Combine(AppContext.BaseDirectory, "tools", "ACE.exe"),
            Path.Combine(AppContext.BaseDirectory, "ACE.exe"),
        };
        return paths.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Checks if ACE is available.
    /// </summary>
    public static bool IsAvailable(string toolsDir) => FindAce(toolsDir) != null;
}
