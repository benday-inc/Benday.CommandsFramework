using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// The tool's own options, with the output and input providers pointed at the interface.
/// </summary>
/// <remarks>
/// A command is handed its output provider when it is built, from the options it is built
/// with, so running a command inside the interface means building it with options that say
/// where the interface is. Everything else forwards to the tool's real options rather than
/// being copied, which matters for the two properties that are caches: the registry and the
/// service provider are built once and shared, and a copy would quietly build a second of
/// each -- and a second service provider means singletons that are not.
/// </remarks>
public sealed class TuiProgramOptions : ICommandProgramOptions
{
    private readonly ICommandProgramOptions _Inner;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="inner">The tool's real options</param>
    /// <param name="outputProvider">Where the command's output goes</param>
    /// <param name="inputProvider">Where the command reads input from</param>
    public TuiProgramOptions(
        ICommandProgramOptions inner,
        ITextOutputProvider outputProvider,
        ITextInputProvider inputProvider)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(outputProvider);
        ArgumentNullException.ThrowIfNull(inputProvider);

        _Inner = inner;
        OutputProvider = outputProvider;
        InputProvider = inputProvider;
    }

    /// <summary>
    /// Where a command's output goes. This is the one thing these options exist to change.
    /// </summary>
    public ITextOutputProvider OutputProvider { get; set; }

    /// <summary>
    /// Where a command reads input from.
    /// </summary>
    public ITextInputProvider InputProvider { get; set; }

    public string ApplicationName
    {
        get => _Inner.ApplicationName;
        set => _Inner.ApplicationName = value;
    }

    public DisplayUsageOptions DisplayUsageOptions
    {
        get => _Inner.DisplayUsageOptions;
        set => _Inner.DisplayUsageOptions = value;
    }

    public string Version
    {
        get => _Inner.Version;
        set => _Inner.Version = value;
    }

    public string Website
    {
        get => _Inner.Website;
        set => _Inner.Website = value;
    }

    public string ConfigurationFolderName
    {
        get => _Inner.ConfigurationFolderName;
        set => _Inner.ConfigurationFolderName = value;
    }

    public bool UsesConfiguration
    {
        get => _Inner.UsesConfiguration;
        set => _Inner.UsesConfiguration = value;
    }

    /// <summary>
    /// The tool's registry. Forwarded rather than copied so a registry built while a command
    /// runs is the one the rest of the interface already has.
    /// </summary>
    public CommandRegistry? CommandRegistry
    {
        get => _Inner.CommandRegistry;
        set => _Inner.CommandRegistry = value;
    }

    public IServiceCollection? ServiceCollection
    {
        get => _Inner.ServiceCollection;
        set => _Inner.ServiceCollection = value;
    }

    /// <summary>
    /// The tool's service provider. Forwarded for the same reason the registry is, and with
    /// more at stake: a second provider means a second set of singletons.
    /// </summary>
    public IServiceProvider? ServiceProvider
    {
        get => _Inner.ServiceProvider;
        set => _Inner.ServiceProvider = value;
    }

    public bool StrictArgumentValidation
    {
        get => _Inner.StrictArgumentValidation;
        set => _Inner.StrictArgumentValidation = value;
    }

    public ArgumentSyntax ArgumentSyntax => _Inner.ArgumentSyntax;

    public ITuiHost? TuiHost => _Inner.TuiHost;

    public bool WarnOnDeprecatedArgumentSyntax => _Inner.WarnOnDeprecatedArgumentSyntax;
}
