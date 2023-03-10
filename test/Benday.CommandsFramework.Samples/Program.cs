using Benday.CommandsFramework;

var util = new CommandAttributeUtility();

if (args.Length == 0)
{
    DisplayUsage(util);
}
else
{
    var command = util.GetCommand(args, typeof(Program).Assembly);

    if (command == null)
    {
        DisplayUsage(util);
    }
    else
    {
        var attr = util.GetCommandAttributeForCommandName(typeof(Program).Assembly,
            command.ExecutionInfo.CommandName);

        if (attr == null)
        {
            throw new InvalidOperationException(
                $"Could not get command attribute for command name '{command.ExecutionInfo.CommandName}'.");
        }
        else
        {
            if (attr.IsAsync == false)
            {
                var runThis = command as ISynchronousCommand;

                if (runThis == null)
                {
                    throw new InvalidOperationException($"Could not convert type to {typeof(ISynchronousCommand)}.");
                }
                else
                {
                    runThis.Execute();
                }
            }
            else
            {
                var runThis = command as IAsyncCommand;

                if (runThis == null)
                {
                    throw new InvalidOperationException($"Could not convert type to {typeof(IAsyncCommand)}.");
                }
                else
                {
                    var temp = runThis.ExecuteAsync().GetAwaiter();

                    temp.GetResult();
                }
            }
        }
    }
}

void DisplayUsage(CommandAttributeUtility util)
{
    var assembly = typeof(Program).Assembly;

    var commands = util.GetAvailableCommandNames(assembly);

    foreach (var command in commands)
    {
        Console.WriteLine(command);
    }
}