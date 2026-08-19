namespace Benday.CommandsFramework;

/// <summary>
/// Add this attribute to a command class to create an alternate name for the command
/// that also supplies argument values. This is useful for creating a shortcut for a
/// command that is usually run with the same set of arguments.
/// <code>
/// [Command(Name = "deploy")]
/// [CommandAlias("deploy-prod", "environment=production", "verbose=true",
///     Description = "Deploy to production")]
/// public class DeployCommand : Command
/// </code>
/// The argument values are applied as though they had been typed on the command line,
/// so anything actually supplied on the command line wins over them. The resulting
/// order of precedence is: command line, then alias, then configuration, then the
/// argument's default value.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CommandAliasAttribute : Attribute
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">The alias that gets typed on the command line</param>
    /// <param name="arguments">Argument values supplied by this alias, each in
    /// 'name=value' form. An entry with no '=' is treated as an argument with an empty
    /// value, which is how flag style boolean arguments are supplied.</param>
    public CommandAliasAttribute(string name, params string[] arguments)
    {
        Name = name;
        Arguments = arguments ?? [];
    }

    /// <summary>
    /// The alias that gets typed on the command line in place of the command name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Argument values supplied by this alias, each in 'name=value' form.
    /// </summary>
    public string[] Arguments { get; }

    /// <summary>
    /// Human readable description of what this alias does. Shown in the list of
    /// available command aliases.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Parses the 'name=value' entries in Arguments into argument names and values.
    /// </summary>
    /// <returns>Argument names and values supplied by this alias</returns>
    public Dictionary<string, string> GetArgumentValues()
    {
        var returnValue = new Dictionary<string, string>();

        foreach (var argument in Arguments)
        {
            if (string.IsNullOrWhiteSpace(argument) == true)
            {
                continue;
            }

            var separatorIndex = argument.IndexOf('=');

            if (separatorIndex < 0)
            {
                // no value, which is how a flag style boolean argument is supplied
                returnValue[argument] = string.Empty;
            }
            else
            {
                var name = argument.Substring(0, separatorIndex);
                var value = argument.Substring(separatorIndex + 1);

                returnValue[name] = value;
            }
        }

        return returnValue;
    }
}
