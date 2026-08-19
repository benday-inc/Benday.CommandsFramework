using System.Reflection;
using System.Text;

namespace Benday.CommandsFramework;

/// <summary>
/// Reports which stored configuration values this tool's commands read, whether each one is
/// set, and which commands want it.
/// </summary>
/// <remarks>
/// This is free once arguments declare that they read from configuration: the same
/// declaration that lets validation say "store it with set-configuration" also says what a
/// working setup looks like. Before, the only way to find out what a tool needed was to run a
/// command and see what it complained about.
/// </remarks>
[Command(Name = CommandFrameworkConstants.CommandName_CheckConfig,
    Description = "Check which configuration values this tool needs and whether they are set",
    Category = CommandFrameworkConstants.CategoryName_Configuration)]
public class CheckConfigurationCommand : Command
{
    public CheckConfigurationCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddBoolean(ArgumentName_MissingOnly)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Only report the values that are not set");

        return args;
    }

    public const string ArgumentName_MissingOnly = "missingonly";

    /// <summary>
    /// True when every required configuration value is set. Readable by a caller that ran
    /// this command in process.
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// The configuration values this tool's commands read, in name order.
    /// </summary>
    public List<ConfigurationRequirement> Requirements { get; } = new();

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var missingOnly = Arguments.GetBooleanValue(ArgumentName_MissingOnly);

        Requirements.AddRange(GetRequirements());

        IsComplete = Requirements.Any(x => x.IsRequired == true && x.IsSet == false) == false;

        if (Requirements.Count == 0)
        {
            WriteLine("This tool does not read any values from stored configuration.");

            return Task.CompletedTask;
        }

        var shown = missingOnly == true
            ? Requirements.Where(x => x.IsSet == false).ToList()
            : Requirements;

        if (shown.Count == 0)
        {
            WriteLine("Every configuration value this tool reads is set.");

            return Task.CompletedTask;
        }

        foreach (var requirement in shown)
        {
            var status = requirement.IsSet == true ? "set" : "NOT SET";
            var necessity = requirement.IsRequired == true ? "required" : "optional";

            WriteLine($"{requirement.Name} - {status} ({necessity})");
            WriteLine($"    used by: {string.Join(", ", requirement.CommandNames)}");

            if (requirement.IsSet == false)
            {
                WriteLine(
                    $"    set it with: {CommandFrameworkConstants.CommandName_SetConfig} " +
                    $"/{CommandFrameworkConstants.CommandArgName_ConfigName}:{requirement.Name} " +
                    $"/{CommandFrameworkConstants.CommandArgName_ConfigValue}:value");
            }
        }

        return Task.CompletedTask;
    }

    private List<ConfigurationRequirement> GetRequirements()
    {
        var utility = new CommandAttributeUtility(ExecutionInfo.Options);

        var assembly = ExecutionInfo.Options.CommandRegistry?.PrimaryAssembly ??
            Assembly.GetEntryAssembly() ??
            GetType().Assembly;

        var byName = new Dictionary<string, ConfigurationRequirement>(
            ArgumentCollection.ArgumentNameComparer);

        foreach (var usage in utility.GetAllCommandUsages(assembly))
        {
            foreach (var argument in usage.Arguments)
            {
                if (argument.IsFromConfig == false)
                {
                    continue;
                }

                if (byName.TryGetValue(argument.Name, out var existing) == false)
                {
                    existing = new ConfigurationRequirement(argument.Name)
                    {
                        IsSet = ExecutionInfo.HasConfiguration == true &&
                            ExecutionInfo.Configuration.HasValue(argument.Name)
                    };

                    byName[argument.Name] = existing;
                }

                // required for one command is enough to make it worth setting
                existing.IsRequired = existing.IsRequired || argument.IsRequired;
                existing.CommandNames.Add(usage.Name);
            }
        }

        return [.. byName.Values.OrderBy(x => x.Name, ArgumentCollection.ArgumentNameComparer)];
    }
}

/// <summary>
/// One stored configuration value that a tool's commands read.
/// </summary>
public sealed class ConfigurationRequirement
{
    public ConfigurationRequirement(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The configuration value's name, which is the argument's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// True when at least one command requires it.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// True when the value is stored.
    /// </summary>
    public bool IsSet { get; set; }

    /// <summary>
    /// The commands that read it.
    /// </summary>
    public SortedSet<string> CommandNames { get; } = new(StringComparer.OrdinalIgnoreCase);
}
