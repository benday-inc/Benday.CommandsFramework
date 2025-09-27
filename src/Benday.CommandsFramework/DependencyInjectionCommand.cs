using Benday.CommandsFramework;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

/// <summary>
/// Base class for commands that use dependency injection.
/// </summary>
public abstract class DependencyInjectionCommand : AsynchronousCommand
{
    protected DependencyInjectionCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider)
    {
        if (info.Options.ServiceCollection == null)
        {
            throw new InvalidOperationException("Service collection was not populated.  HINT: check Program.cs");
        }
    }

    private IServiceScope? _ServiceScope;

    private IServiceScope Scope
    {
        get
        {
            if (_ServiceScope == null)
            {
                var services = ExecutionInfo.Options.ServiceCollection ??
                    throw new InvalidOperationException("Service collection was not populated.  HINT: check Program.cs");

                var serviceProvider = services.BuildServiceProvider();

                _ServiceScope = serviceProvider.CreateScope();
            }

            return _ServiceScope;
        }
    }

    /// <summary>
    /// Get a required service instance from the service provider.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected T GetRequiredService<T>() where T : notnull
    {
        var returnValue = Scope.ServiceProvider.GetRequiredService<T>();

        return returnValue;
    }
}