using System.Text;
using SimaiAutomator.Cli.Tui;

// Force UTF-8 for CJK characters in Windows console
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

await App.RunAsync();
