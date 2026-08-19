using System.Reflection;

namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Tests that the list of commands the user is shown never disagrees with the list of
/// commands the framework can actually create. When the two disagreed, a CommandAttribute
/// on a class that was not a CommandBase was listed in the command list and then threw
/// MissingArgumentException out of the --json schema dump, taking the whole dump -- and
/// cmdui with it -- down over one bad class.
/// </summary>
public class CommandTypeDiscoveryFixture
{
    private const string RunnableCommandName = "runnable-command";
    private const string NotACommandBaseCommandName = "not-a-commandbase";
    private const string AbstractCommandName = "abstract-command";

    [Command(Name = RunnableCommandName, Description = "A command that the framework can run")]
    private class RunnableCommand : SynchronousCommand
    {
        public RunnableCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }

        public override ArgumentCollection GetArguments()
        {
            return new ArgumentCollection();
        }

        protected override void OnExecute()
        {
        }
    }

    /// <summary>
    /// The defect in one class: it has the attribute but it is not a CommandBase, so it
    /// can be listed but never instantiated.
    /// </summary>
    [Command(Name = NotACommandBaseCommandName, Description = "Has the attribute but is not a command")]
    private class NotACommandBase
    {
    }

    /// <summary>
    /// Same failure by a different route -- a CommandBase that cannot be constructed.
    /// </summary>
    [Command(Name = AbstractCommandName, Description = "Cannot be constructed")]
    private abstract class AbstractCommand : SynchronousCommand
    {
        protected AbstractCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
            : base(info, outputProvider)
        {
        }
    }

    private static Assembly ThisAssembly => typeof(CommandTypeDiscoveryFixture).Assembly;

    private static CommandAttributeUtility SystemUnderTest =>
        new CommandAttributeUtility(new DefaultProgramOptions
        {
            ApplicationName = "Test Sample Application",
            ConfigurationFolderName = "TestSampleApplication-Deleteable",
            UsesConfiguration = false
        });

    [Fact]
    public void AvailableCommandNames_ExcludeTypesThatAreNotCommands()
    {
        // act
        var actual = SystemUnderTest.GetAvailableCommandNames(ThisAssembly);

        // assert
        Assert.Contains(RunnableCommandName, actual);
        Assert.DoesNotContain(NotACommandBaseCommandName, actual);
        Assert.DoesNotContain(AbstractCommandName, actual);
    }

    [Fact]
    public void EveryListedCommandCanBeInstantiated()
    {
        // arrange
        var utility = SystemUnderTest;

        // act & assert -- this is the assertion that the --json dump depends on
        foreach (var name in utility.GetAvailableCommandNames(ThisAssembly))
        {
            Assert.NotNull(utility.GetAvailableCommandType(ThisAssembly, name));
        }
    }

    [Fact]
    public void GetAllCommandUsages_SurvivesATypeThatIsNotACommand()
    {
        // act -- before the fix this threw MissingArgumentException
        var actual = SystemUnderTest.GetAllCommandUsages(ThisAssembly);

        // assert
        Assert.Contains(actual, x => x.Name == RunnableCommandName);
        Assert.DoesNotContain(actual, x => x.Name == NotACommandBaseCommandName);
        Assert.DoesNotContain(actual, x => x.Name == AbstractCommandName);
    }

    [Fact]
    public void CommandNameProblems_ReportTypesThatAreNotRunnableCommands()
    {
        // act
        var actual = SystemUnderTest.GetCommandNameProblems(ThisAssembly);

        // assert -- skipping them silently would leave the author with no idea why the
        // command never showed up, so they are reported instead
        Assert.Contains(actual, x => x.Contains(NotACommandBaseCommandName));
        Assert.Contains(actual, x => x.Contains(AbstractCommandName));
    }

    [Fact]
    public void IsCommandType_Values()
    {
        Assert.True(CommandAttributeUtility.IsCommandType(typeof(RunnableCommand)));
        Assert.False(CommandAttributeUtility.IsCommandType(typeof(NotACommandBase)));
        Assert.False(CommandAttributeUtility.IsCommandType(typeof(AbstractCommand)));
        Assert.False(CommandAttributeUtility.IsCommandType(typeof(CommandTypeDiscoveryFixture)));
    }
}
