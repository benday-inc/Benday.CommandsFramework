using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// Puts text on the system clipboard by handing it to whichever clipboard tool the machine
/// has.
/// </summary>
/// <remarks>
/// .NET has no clipboard of its own outside of a desktop UI framework, and a terminal
/// application has no business referencing one. Every platform ships a small command line
/// tool for this, so this finds one and pipes to it.
///
/// Failing is expected and is not an error: a machine over SSH with no clipboard tool
/// installed simply has no clipboard, and the text is on screen anyway.
/// </remarks>
public static class Clipboard
{
    /// <summary>
    /// The clipboard tools this knows about, in the order they are tried, for the platform
    /// it is running on.
    /// </summary>
    /// <remarks>
    /// Wayland before X11 on Linux: wl-copy works on a Wayland session and xclip does not,
    /// while xclip usually still works under XWayland, so trying the more specific one first
    /// is what gets it right on both.
    /// </remarks>
    public static IReadOnlyList<ClipboardTool> GetCandidateTools()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == true)
        {
            return [new ClipboardTool("pbcopy", [])];
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true)
        {
            return [new ClipboardTool("clip", [])];
        }

        return
        [
            new ClipboardTool("wl-copy", []),
            new ClipboardTool("xclip", ["-selection", "clipboard"]),
            new ClipboardTool("xsel", ["--clipboard", "--input"])
        ];
    }

    /// <summary>
    /// Tries to put text on the clipboard.
    /// </summary>
    /// <param name="text">What to copy</param>
    /// <returns>True when a clipboard tool took it</returns>
    public static bool TryCopy(string text)
    {
        text ??= string.Empty;

        foreach (var tool in GetCandidateTools())
        {
            if (TryCopyWith(tool, text) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryCopyWith(ClipboardTool tool, string text)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = tool.FileName,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in tool.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return false;
            }

            process.StandardInput.Write(text);
            process.StandardInput.Close();

            process.WaitForExit(5000);

            return process.HasExited == true && process.ExitCode == 0;
        }
        catch
        {
            // the tool is not installed, or is not allowed to run. Either way there is no
            // clipboard here, which is not a failure worth reporting as one.
            return false;
        }
    }
}

/// <summary>
/// One clipboard tool and the arguments it needs to write to the system clipboard.
/// </summary>
/// <param name="FileName">The executable</param>
/// <param name="Arguments">Its arguments</param>
public sealed record ClipboardTool(string FileName, IReadOnlyList<string> Arguments);
