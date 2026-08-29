using System.Diagnostics;
using System.Reflection;

namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// Runs a command from the interface, in this process.
/// </summary>
/// <remarks>
/// The command is built from the command line the form shows, through the ordinary run path,
/// which is what makes the preview verifiable rather than decorative: what runs is what the
/// preview says would run, parsed by the same parser that would parse it had it been typed.
///
/// A fresh command is built for each run. Building it is what applies the values, and it is
/// also what gives each run its own dependency injection scope -- and the scope is disposed
/// when the run ends, which in a process that runs many commands is the difference between a
/// scope and a leak.
/// </remarks>
public sealed class TuiCommandRunner
{
    private readonly TuiProgramOptions _Options;
    private readonly Assembly _CommandsAssembly;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">The tool's options</param>
    /// <param name="commandsAssembly">Assembly holding the commands</param>
    /// <param name="output">Where output goes, or null for a new one</param>
    /// <param name="input">Where input comes from, or null for a new one</param>
    public TuiCommandRunner(
        ICommandProgramOptions options,
        Assembly commandsAssembly,
        TuiTextOutputProvider? output = null,
        TuiTextInputProvider? input = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(commandsAssembly);

        Output = output ?? new TuiTextOutputProvider();
        Input = input ?? new TuiTextInputProvider(Output);
        _CommandsAssembly = commandsAssembly;

        _Options = new TuiProgramOptions(options, Output, Input);
    }

    /// <summary>
    /// Where a running command's output goes.
    /// </summary>
    public TuiTextOutputProvider Output { get; }

    /// <summary>
    /// Where a running command reads input from.
    /// </summary>
    public TuiTextInputProvider Input { get; }

    /// <summary>
    /// True while a command is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Runs what a form has been filled in with.
    /// </summary>
    /// <param name="form">The filled in form</param>
    /// <param name="cancellationToken">Cancels the command. Cancelling a command does not
    /// mean closing the interface.</param>
    /// <returns>What happened</returns>
    public Task<TuiRunResult> RunAsync(
        TuiCommandForm form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        return RunAsync(form.GetCommandLineTokens(), cancellationToken);
    }

    /// <summary>
    /// Runs a command line.
    /// </summary>
    /// <param name="tokens">The command name and its arguments, as they would be typed</param>
    /// <param name="cancellationToken">Cancels the command</param>
    /// <returns>What happened</returns>
    public async Task<TuiRunResult> RunAsync(
        string[] tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        if (tokens.Length == 0)
        {
            throw new ArgumentException(
                "There is no command to run.", nameof(tokens));
        }

        Output.Clear();

        var timer = Stopwatch.StartNew();

        IsRunning = true;

        try
        {
            return await ExecuteAsync(tokens, timer, cancellationToken);
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task<TuiRunResult> ExecuteAsync(
        string[] tokens, Stopwatch timer, CancellationToken cancellationToken)
    {
        var utility = new CommandAttributeUtility(_Options);

        try
        {
            // the command owns a dependency injection scope, so the runner disposes it when
            // the run is over
            using var command = utility.GetCommand(tokens, _CommandsAssembly);

            if (command is null)
            {
                return Finish(
                    CommandResult.Failed($"There is no command named '{tokens[0]}'."), timer);
            }

            if (command is not Command runnable)
            {
                return Finish(
                    CommandResult.Failed(
                        $"'{tokens[0]}' does not derive from {nameof(Command)}, " +
                        "so there is nothing to run."),
                    timer);
            }

            // never quiet. Quiet mode suppresses WriteLine(), which is the output the
            // interface exists to show.
            return Finish(await runnable.ExecuteAsync(cancellationToken), timer);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Finish(CommandResult.Cancelled(), timer);
        }
        catch (KnownException ex)
        {
            return Finish(CommandResult.Failed(ex.Message), timer);
        }
        catch (Exception ex)
        {
            // deliberately wider than the console entry point, which lets an unexpected
            // exception escape and end the process. Here the process is the interface, and a
            // command that throws should cost the user that command rather than everything
            // they had on the screen.
            return Finish(
                CommandResult.Failed($"{ex.GetType().Name}: {ex.Message}"), timer, ex);
        }
    }

    /// <summary>
    /// Packages the result with what the command wrote and how long it took.
    /// </summary>
    private TuiRunResult Finish(
        CommandResult result, Stopwatch timer, Exception? exception = null)
    {
        timer.Stop();

        return new TuiRunResult(result, Output.Lines, timer.Elapsed, exception);
    }
}
