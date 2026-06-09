using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SimaiAutomator.Core.Services;

/// <summary>
/// Fully automated ACE GUI control via Win32 API.
/// Replaces audio in an ACB file without user interaction.
/// </summary>
public static class AceAutomator
{
    #region Win32 P/Invoke

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hwndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const uint WM_CLOSE = 0x0010;
    private const uint WM_COMMAND = 0x0111;
    private const uint WM_SETTEXT = 0x000C;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint BM_CLICK = 0x00F5;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const byte VK_RETURN = 0x0D;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_S = 0x53;
    private const byte VK_O = 0x4F;
    private const byte VK_TAB = 0x09;
    private const byte KEYEVENTF_KEYUP = 0x0002;

    #endregion

    /// <summary>
    /// Fully automated ACE audio replacement.
    /// 1. Opens ACE with template ACB
    /// 2. Uses Ctrl+O to open file
    /// 3. Uses Alt menus and keyboard shortcuts to replace audio
    /// 4. Saves to output path
    /// </summary>
    public static async Task<bool> ReplaceAudioAsync(
        string toolsDir,
        string templateAcbPath,
        string audioPath,
        string outputAcbPath,
        string outputAwbPath)
    {
        var aceExe = AudioConverter.FindAce(toolsDir);
        if (aceExe == null) return false;

        // Copy template AWB if exists
        var templateAwb = Path.ChangeExtension(templateAcbPath, ".awb");
        if (File.Exists(templateAwb))
            File.Copy(templateAwb, outputAwbPath, true);

        // Launch ACE with template
        var psi = new ProcessStartInfo
        {
            FileName = aceExe,
            Arguments = $"\"{templateAcbPath}\"",
            UseShellExecute = true
        };

        using var proc = Process.Start(psi);
        if (proc == null) return false;

        // Wait for ACE window to appear
        IntPtr hwnd = IntPtr.Zero;
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(500);
            hwnd = FindWindow(null, "Audio Cue Editor");
            if (hwnd != IntPtr.Zero) break;
        }

        if (hwnd == IntPtr.Zero)
        {
            Console.WriteLine("ACE window not found. Please complete audio manually.");
            await proc.WaitForExitAsync();
            return File.Exists(outputAcbPath);
        }

        SetForegroundWindow(hwnd);
        await Task.Delay(500);

        // Step 1: Open the template ACB via Ctrl+O
        // Actually ACE might already have it loaded from args. Let's skip to Replace.

        // Step 2: Navigate to the audio cue in the tree
        // Press Tab to focus the tree, then Down to select first cue
        keybd_event(VK_TAB, 0, 0, UIntPtr.Zero);
        await Task.Delay(200);
        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        await Task.Delay(100);

        // Press Down to select first child in tree
        PressKey(VK_RETURN); // Expand
        await Task.Delay(200);

        // Step 3: Open Replace dialog via Alt+A, R (menu: Action -> Replace)
        PressAltKey('A');
        await Task.Delay(300);
        PressKey((byte)'R');
        await Task.Delay(500);

        // Step 4: In the Replace dialog, paste the audio path
        // The dialog should be open now with a file path textbox focused
        // Just type the audio file path directly
        await Task.Delay(300);
        TypeText(audioPath);
        await Task.Delay(300);

        // Step 5: Press Enter to confirm (OK button)
        PressKey(VK_RETURN);
        await Task.Delay(1000);

        // Step 6: Save As (Ctrl+Shift+S or Alt+F, A)
        PressCtrlKey('S');
        await Task.Delay(800);

        // Step 7: In Save dialog, type output path
        TypeText(outputAcbPath);
        await Task.Delay(300);
        PressKey(VK_RETURN);
        await Task.Delay(1000);

        // Step 8: Close ACE
        PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        await Task.Delay(500);

        // If ACE is still running, kill it
        if (!proc.HasExited)
        {
            try { proc.Kill(); } catch { }
        }

        return File.Exists(outputAcbPath);
    }

    private static void PressKey(byte vk)
    {
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void PressCtrlKey(char c)
    {
        byte vk = (byte)char.ToUpper(c);
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void PressAltKey(char c)
    {
        byte vk = (byte)char.ToUpper(c);
        keybd_event(0x12, 0, 0, UIntPtr.Zero); // Alt down
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Alt up
    }

    private static void TypeText(string text)
    {
        foreach (char c in text)
        {
            short vk = VkKeyScan(c);
            byte key = (byte)(vk & 0xFF);
            bool shift = (vk >> 8 & 1) != 0;

            if (shift) keybd_event(0x10, 0, 0, UIntPtr.Zero); // Shift down
            keybd_event(key, 0, 0, UIntPtr.Zero);
            keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Shift up
        }
    }

    public static bool IsAvailable(string toolsDir) => AudioConverter.FindAce(toolsDir) != null;
}
