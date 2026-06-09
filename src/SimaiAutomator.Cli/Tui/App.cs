using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;
using SimaiAutomator.Core.Services;
using SimaiAutomator.Core.Models;

namespace SimaiAutomator.Cli.Tui;

public static class App
{
    private const string DefaultOpt = "A500";

    public static async Task RunAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Simai Auto").Color(Color.Green));
        AnsiConsole.Write(new Rule("[green]maimai 自制谱一键上机工具[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        var inputType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]谱面文件是？[/]")
                .AddChoices(["文件夹", "压缩包 (.zip)"]));

        string projectDir;
        if (inputType.Contains("zip"))
            projectDir = await PickZipAsync();
        else
            projectDir = PickFolder();

        if (string.IsNullOrEmpty(projectDir))
        {
            AnsiConsole.MarkupLine("[red]未选择文件，退出。[/]");
            return;
        }

        var maidataPath = Path.Combine(projectDir, "maidata.txt");
        if (!File.Exists(maidataPath))
        {
            AnsiConsole.MarkupLine($"[red]找不到 maidata.txt: {E(projectDir)}[/]");
            PauseAndExit();
            return;
        }

        SimaiProject project;
        try { project = MaidataParser.Parse(maidataPath); }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]解析失败: {E(ex.Message)}[/]");
            PauseAndExit();
            return;
        }

        ShowSummary(project);

        var id6 = AnsiConsole.Ask<int>(
            "[bold]请输入 6 位数 ID[/] ([grey]DX=01xxxx, 标准=00xxxx[/]):");

        if (id6 < 0 || id6 > 199999 || (id6 >= 100000 && id6 < 10000))
        {
            AnsiConsole.MarkupLine("[red]ID 格式错误，6 位数，DX=01xxxx 标准=00xxxx[/]");
            PauseAndExit();
            return;
        }

        var isDx = (id6 / 10000) % 10 == 1;
        AnsiConsole.MarkupLine(isDx
            ? $"[blue]DX: {id6:D6}[/]"
            : $"[blue]标准: {id6:D6}[/]");

        if (!AnsiConsole.Confirm("[bold]确认开始转换?[/]"))
        {
            AnsiConsole.MarkupLine("[grey]已取消。[/]");
            return;
        }

        AnsiConsole.WriteLine();
        var toolsDir = FindToolsDir();
        var outputBase = Path.Combine(AppContext.BaseDirectory, "output");
        var pipeline = new Pipeline(toolsDir, outputBase);

        if (!AudioConverter.IsAvailable(toolsDir))
            AnsiConsole.MarkupLine("[yellow]未检测到 ACE.exe，音频将跳过[/]");

        string? resultPath = null;
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]转换中...[/]");
                try
                {
                    resultPath = await pipeline.RunAsync(projectDir, id6, DefaultOpt, msg =>
                        task.Description = $"[grey]{E(msg)}[/]");

                    task.Description = "[green]完成![/]";
                    task.StopTask();
                }
                catch (Exception ex)
                {
                    task.Description = $"[red]失败: {E(ex.Message)}[/]";
                    task.StopTask();
                }
            });

        AnsiConsole.WriteLine();

        if (resultPath != null && Directory.Exists(resultPath))
        {
            AnsiConsole.MarkupLine($"[bold green]输出:[/] {E(resultPath)}");
        }

        PauseAndExit();
    }

    private static void PauseAndExit()
    {
        AnsiConsole.MarkupLine("[grey]按任意键退出...[/]");
        Console.ReadKey(true);
        Environment.Exit(0);
    }

    private static string E(string s) => Markup.Escape(s);

    private static void ShowSummary(SimaiProject project)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("[bold]标题[/]", E(project.Title));
        grid.AddRow("[bold]作者[/]", E(project.Artist));
        grid.AddRow("[bold]BPM[/]", E(project.Bpm.ToString()));

        var validNums = project.ValidSimaiNumbers;
        var chartStr = validNums.Count > 0
            ? string.Join(", ", validNums.Select(n =>
            {
                var e2 = project.Charts[n];
                var slot = validNums.IndexOf(n);
                return $"{((ChartDifficulty)slot).Label()} Lv.{e2.LevelDisplay}";
            }))
            : "无";
        grid.AddRow("[bold]谱面[/]", E(chartStr));

        if (project.FirstOffset > 0)
            grid.AddRow("[bold]偏移[/]", E($"{project.FirstOffset}s"));

        grid.AddRow("[bold]音频[/]", project.AudioPath != null ? "有" : "无");
        grid.AddRow("[bold]封面[/]", project.CoverPath != null ? "有" : "无");
        grid.AddRow("[bold]BGA[/]", project.BgaPath != null ? "有" : "无");

        var panel = new Panel(grid)
        {
            Header = new PanelHeader($"[bold yellow]{E(project.Title)}[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private static string PickFolder()
    {
        AnsiConsole.MarkupLine("[grey]将打开文件夹选择器...[/]");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; $f = New-Object System.Windows.Forms.FolderBrowserDialog; $f.Description = 'Select folder with maidata.txt'; if ($f.ShowDialog() -eq 'OK') { $f.SelectedPath }\"",
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd().Trim();
            proc?.WaitForExit();
            if (!string.IsNullOrEmpty(output)) return output;
        }
        catch { }
        return AnsiConsole.Ask<string>("[bold]请输入谱面文件夹路径:[/]");
    }

    private static async Task<string> PickZipAsync()
    {
        AnsiConsole.MarkupLine("[grey]将打开文件选择器...[/]");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; $f = New-Object System.Windows.Forms.OpenFileDialog; $f.Filter = 'ZIP files (*.zip)|*.zip'; $f.Title = 'Select chart ZIP'; if ($f.ShowDialog() -eq 'OK') { $f.FileName }\"",
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd().Trim();
            proc?.WaitForExit();
            if (string.IsNullOrEmpty(output)) return string.Empty;

            var tempDir = Path.Combine(Path.GetTempPath(), $"simai_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(output, tempDir);
            var maidata = Directory.GetFiles(tempDir, "maidata.txt", SearchOption.AllDirectories).FirstOrDefault();
            return maidata != null ? Path.GetDirectoryName(maidata)! : tempDir;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]解压失败: {E(ex.Message)}[/]");
        }
        return AnsiConsole.Ask<string>("[bold]请输入谱面文件夹路径:[/]");
    }

    private static string FindToolsDir()
    {
        var dirs = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tools"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "tools"),
            Path.Combine(AppContext.BaseDirectory, "..", "tools"),
        };
        return dirs.FirstOrDefault(Directory.Exists) ?? Path.Combine(AppContext.BaseDirectory, "tools");
    }
}
