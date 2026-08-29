namespace Benday.CommandsFramework;

/// <summary>
/// Base class for commands that use dependency injection.
/// </summary>
/// <remarks>
/// Commands are created through ActivatorUtilities now, so any command can declare the
/// services it needs as constructor parameters, and GetRequiredService&lt;T&gt;() lives on
/// CommandBase for the cases that cannot. That supersedes this class rather than flattening
/// it -- it is kept so existing code compiles, and it adds nothing.
///
/// Absorbing it cost nothing: the service scope is created on first use, so a command that
/// never touches dependency injection never creates one.
/// </remarks>
[Obsolete("Derive from Command instead. Any command can take its dependencies as constructor parameters, and GetRequiredService<T>() is on CommandBase. This will be removed in v6.")]
public abstract class DependencyInjectionCommand : Command
{
    protected DependencyInjectionCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider)
    {
    }
}
