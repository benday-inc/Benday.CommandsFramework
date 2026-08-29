using Benday.CommandsFramework;

namespace Benday.CommandsFramework.Samples;

/// <summary>
/// Sample command demonstrating progress reporting. Progress is commentary about the work
/// rather than the work's result, so it goes to the diagnostic channel -- which is what lets
/// the result be redirected to a file without the progress ending up inside it.
/// </summary>
[Command(Name = ApplicationConstants.CommandName_Progress,
    Description = "Sample command demonstrating progress reporting.")]
public class SampleProgressCommand : Command
{
    public const string ArgumentName_Count = "count";

    public SampleProgressCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddInt32(ArgumentName_Count)
            .AsNotRequired()
            .WithDefaultValue(5)
            .WithDescription("How many items to pretend to process");

        return args;
    }

    protected override Task OnExecute(CancellationToken cancellationToken)
    {
        var count = Arguments.GetInt32Value(ArgumentName_Count);

        ReportProgress("Starting");

        for (var i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReportProgress($"Processing item {i}", i, count);
        }

        // the result goes to the result channel, so 'mytool progress > out.txt' captures
        // this line and nothing else
        WriteLine($"Processed {count} items.");

        return Task.CompletedTask;
    }
}
