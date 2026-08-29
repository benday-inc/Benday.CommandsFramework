using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample commands that live under a group, so they are run as 'widget list' rather than
/// 'widgetlist'. Group is deliberately separate from Category: Category holds display strings
/// like "Widget Management" that nobody would type, so using it as a prefix would produce
/// command names no one could guess. Grouping is a rename, not a prefix.
/// </summary>
[Command(
    Group = ApplicationConstants.CommandGroup_Widget,
    Name = ApplicationConstants.CommandName_WidgetList,
    Category = "Widget Management",
    Description = "Lists the widgets.")]
public class SampleWidgetListCommand : Command
{
    public SampleWidgetListCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("filter")
            .AsNotRequired()
            .WithDescription("Only list widgets whose name contains this");

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        WriteLine("** SUCCESS **");
        WriteLine($"listing widgets, filter: '{Arguments.GetStringValue("filter")}'");

        return Task.CompletedTask;
    }
}

/// <summary>
/// A grouped command that keeps its old flat name working as an alias. This is how an
/// existing tool adopts groups without breaking anyone's scripts.
/// </summary>
[Command(
    Group = ApplicationConstants.CommandGroup_Widget,
    Name = ApplicationConstants.CommandName_WidgetShow,
    Category = "Widget Management",
    Description = "Shows one widget.",
    Aliases = [ApplicationConstants.CommandAlias_ShowWidget])]
public class SampleWidgetShowCommand : Command
{
    public SampleWidgetShowCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("name")
            .AsRequired()
            .WithDescription("Name of the widget to show");

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        WriteLine("** SUCCESS **");
        WriteLine($"widget: {Arguments.GetStringValue("name")}");

        return Task.CompletedTask;
    }
}
