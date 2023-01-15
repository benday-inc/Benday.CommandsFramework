using System.Reflection;
using System.Linq;

namespace Benday.CommandsFramework;

public class CommandAttributeUtility
{
    public List<string> GetAvailableCommandNames(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var returnValue = new List<string>();

        var matchingTypes =
            from type in containingAssembly.GetTypes()
            where type.GetCustomAttributes<CommandAttribute>().Any()
            select type;

        foreach (var type in matchingTypes)
        {
            var attr = type.GetCustomAttribute<CommandAttribute>();

            if (attr != null)
            {
                returnValue.Add(attr.Name);
            }
        }

        return returnValue;
    }

    public Type? GetAvailableCommandType(Assembly containingAssembly, string commandName)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var match =
            (from type in containingAssembly.GetTypes()
            where 
                type.IsSubclassOf(typeof(CommandBase)) == true &&
                type.GetCustomAttributes<CommandAttribute>().Any(t=> t.Name == commandName)
            select type).FirstOrDefault();

        return match;
    }

    public CommandBase? GetCommand(string[] args, Assembly containingAssembly)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (args.Length == 0)
        {
            throw new ArgumentException(nameof(args));
        }

        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var execInfo = new ArgumentCollectionFactory().Parse(args);

        if (execInfo is null || string.IsNullOrEmpty(execInfo.CommandName) == true)
        {
            throw new MissingArgumentException("Could not locate a command name.");
        }
        else
        {
            var commandNames = GetAvailableCommandNames(containingAssembly);

            if (commandNames.Contains(execInfo.CommandName) == false)
            {
                throw new MissingArgumentException($"Could not locate a command named '{execInfo.CommandName}'.");
            }
            else
            {
                var commandType = GetAvailableCommandType(containingAssembly, execInfo.CommandName);

                if (commandType is null)
                {
                    throw new MissingArgumentException($"Could not locate a command data type named '{execInfo.CommandName}'.");
                }

                var ctor = commandType.GetConstructor(new Type[] { typeof(CommandExecutionInfo) });

                if (ctor is null)
                {
                    throw new MissingArgumentException($"Could not locate a constructor on command type named '{execInfo.CommandName}'.");
                }

                var instance = ctor.Invoke(new object[] { execInfo });

                return instance as CommandBase;
            }
        }
    }
}
