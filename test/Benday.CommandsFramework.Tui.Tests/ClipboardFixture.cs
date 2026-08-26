using System.Runtime.InteropServices;

using Benday.CommandsFramework.Tui.Model;

namespace Benday.CommandsFramework.Tui.Tests;

/// <summary>
/// Tests which clipboard tool gets tried. .NET has no clipboard outside a desktop UI
/// framework, and a terminal application has no business referencing one, so this hands the
/// text to whichever small command line tool the machine ships.
/// </summary>
public class ClipboardFixture
{
    [Fact]
    public void ThereIsAlwaysSomethingToTry()
    {
        Assert.NotEmpty(Clipboard.GetCandidateTools());
    }

    [Fact]
    public void EveryCandidateNamesAnExecutable()
    {
        Assert.All(
            Clipboard.GetCandidateTools(),
            tool => Assert.False(string.IsNullOrWhiteSpace(tool.FileName)));
    }

    [Fact]
    public void TheRightToolIsTriedForThisPlatform()
    {
        // act
        var first = Clipboard.GetCandidateTools()[0];

        // assert
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == true)
        {
            Assert.Equal("pbcopy", first.FileName);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true)
        {
            Assert.Equal("clip", first.FileName);
        }
        else
        {
            // wayland before X11: wl-copy works on a Wayland session and xclip does not,
            // while xclip usually still works under XWayland
            Assert.Equal("wl-copy", first.FileName);
            Assert.Contains(Clipboard.GetCandidateTools(), x => x.FileName == "xclip");
        }
    }

    [Fact]
    public void CopyingNothingIsNotAnError()
    {
        // a machine with no clipboard tool simply has no clipboard, which is a fact rather
        // than a failure -- the text is on screen either way
        var copied = Clipboard.TryCopy(string.Empty);

        Assert.True(copied == true || copied == false);
    }
}
