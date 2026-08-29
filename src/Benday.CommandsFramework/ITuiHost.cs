namespace Benday.CommandsFramework;

/// <summary>
/// Runs a terminal user interface for a program. This is how the core framework launches a
/// TUI it does not reference.
/// </summary>
/// <remarks>
/// The implementation lives in Benday.CommandsFramework.Tui, which depends on a rendering
/// library. Core depends only on Microsoft.Extensions.*, and that is worth protecting, so
/// what core holds is this one method and nothing else.
///
/// Deliberately unlike 'gui': cmdui is a separate executable, so the gui keyword can offer to
/// install it at run time. TUI support is a compile time reference, so a tool either was built
/// with it or was not, and no amount of installing anything changes that.
/// </remarks>
public interface ITuiHost
{
    /// <summary>
    /// Runs the terminal user interface until the user exits it.
    /// </summary>
    /// <param name="program">The program whose commands the interface shows. Everything the
    /// interface needs -- the options, the registry, the assembly holding the commands --
    /// hangs off this.</param>
    /// <param name="cancellationToken">Stops the interface</param>
    /// <returns>The exit code the process should use if it is exiting</returns>
    Task<int> RunAsync(ICommandProgram program, CancellationToken cancellationToken = default);
}
