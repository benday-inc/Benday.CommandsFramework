using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

public interface ICommandProgramOptions
{
    string ApplicationName { get; set; }
    DisplayUsageOptions DisplayUsageOptions { get; set; }
    string Version { get; set; }
    string Website { get; set; }
    string ConfigurationFolderName { get; set; }
    bool UsesConfiguration { get; set; }
    ITextOutputProvider OutputProvider { get; set; }

    /// <summary>
    /// Where commands read text input from. The counterpart to OutputProvider.
    /// </summary>
    ITextInputProvider InputProvider { get; set; }

    /// <summary>
    /// The set of commands this program can run. Built the first time it is needed and then
    /// shared, the same way ServiceProvider is, so the assemblies are only scanned once.
    /// </summary>
    CommandRegistry? CommandRegistry { get; set; }
    IServiceCollection? ServiceCollection { get; set; }

    /// <summary>
    /// The service provider built from ServiceCollection. This is populated the first
    /// time a command needs it and is then shared by every command in the process so
    /// that singleton services really are singletons and the container is only built once.
    /// </summary>
    IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// When true, unknown/unrecognized command arguments will cause validation to fail.
    /// When false (default), unknown arguments are silently ignored.
    /// </summary>
    bool StrictArgumentValidation { get; set; }

    /// <summary>
    /// Which command line argument syntax this program accepts. Defaults to
    /// <see cref="ArgumentSyntax.Both"/> -- the POSIX form is what gets rendered and the
    /// deprecated slash form still parses.
    /// </summary>
    /// <remarks>
    /// A default interface member, so adding it broke no existing implementation of this
    /// interface. DefaultProgramOptions declares it settable.
    /// </remarks>
    ArgumentSyntax ArgumentSyntax => ArgumentSyntax.Both;

    /// <summary>
    /// What runs when the 'tui' keyword is used. Null means this tool was not built with a
    /// terminal interface.
    /// </summary>
    /// <remarks>
    /// A default interface member, for the same reason InputProvider and ArgumentSyntax are:
    /// adding it broke no existing implementation. DefaultProgramOptions declares it settable,
    /// and the WithTui() extension in Benday.CommandsFramework.Tui is what sets it.
    /// </remarks>
    ITuiHost? TuiHost => null;

    /// <summary>
    /// When true (the default), an argument typed in the deprecated slash form produces a
    /// warning on the diagnostic channel. Set it to false for a tool whose existing scripts
    /// should stay quiet while they are being migrated.
    /// </summary>
    bool WarnOnDeprecatedArgumentSyntax => true;
}
