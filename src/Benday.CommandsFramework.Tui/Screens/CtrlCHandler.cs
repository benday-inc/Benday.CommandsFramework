using Spectre.Console;

namespace Benday.CommandsFramework.Tui.Screens;

/// <summary>
/// Makes Ctrl-C cancel the running command rather than the interface.
/// </summary>
/// <remarks>
/// A command that is cancelled does not have to mean a process that stops, which is exactly
/// what the framework's cancellation contract is for: ExecuteAsync turns a cancelled token
/// into CommandResult.Cancelled and the interface carries on.
///
/// A second press within the same run is left to the runtime, which ends the process. That is
/// the escape hatch for a command that ignores its token: the first press asks, and the
/// second insists.
/// </remarks>
internal sealed class CtrlCHandler : IDisposable
{
    private readonly IAnsiConsole _Console;
    private readonly CancellationTokenSource _Cancellation;
    private int _Presses;

    private CtrlCHandler(IAnsiConsole console, CancellationTokenSource cancellation)
    {
        _Console = console;
        _Cancellation = cancellation;

        System.Console.CancelKeyPress += OnCancelKeyPress;
    }

    /// <summary>
    /// Starts listening for Ctrl-C.
    /// </summary>
    /// <param name="console">Where to say that it is cancelling</param>
    /// <param name="cancellation">What a press cancels</param>
    /// <returns>The handler, or null when this process cannot listen for it</returns>
    public static CtrlCHandler? Hook(
        IAnsiConsole console, CancellationTokenSource cancellation)
    {
        try
        {
            return new CtrlCHandler(console, cancellation);
        }
        catch (Exception)
        {
            // a process with no console to press it in loses nothing but the shortcut
            return null;
        }
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _Presses++;

        if (_Presses > 1)
        {
            // insisted on. e.Cancel stays false, so the runtime ends the process.
            return;
        }

        e.Cancel = true;

        _Console.MarkupLine("[yellow]Cancelling. Press Ctrl-C again to quit.[/]");

        _Cancellation.Cancel();
    }

    public void Dispose()
    {
        System.Console.CancelKeyPress -= OnCancelKeyPress;
    }
}
