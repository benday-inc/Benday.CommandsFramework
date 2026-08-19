namespace Benday.CommandsFramework.Tests;

/// <summary>
/// Every fixture that reads or writes Environment.ExitCode belongs to this collection.
/// </summary>
/// <remarks>
/// The process exit code is global to the test run, so two fixtures asserting on it at the
/// same time is a race -- and xunit runs different test classes in parallel by default. This
/// makes them run one at a time instead. It is a test concern only: the framework itself no
/// longer sets Environment.ExitCode anywhere except the console entry point, which is exactly
/// what several of these tests exist to prove.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public class ProcessExitCodeCollection
{
    public const string Name = "process exit code";
}
