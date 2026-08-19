using Benday.CommandsFramework;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

/// <summary>
/// Base class for commands that use dependency injection.
/// The service collection is validated lazily when services are first accessed,
/// allowing commands to be instantiated for schema discovery without DI configuration.
/// </summary>
public abstract class DependencyInjectionCommand : Command, IDisposable
{
    protected DependencyInjectionCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider)
    {
    }

    private IServiceScope? _ServiceScope;
    private bool _IsDisposed;

    private IServiceScope Scope
    {
        get
        {
            if (_ServiceScope == null)
            {
                var options = ExecutionInfo.Options;

                // the service provider is built once and then shared by every command in
                // the process. Building it per command would give each command its own
                // set of singletons and would rebuild the container every time one
                // command called another.
                var serviceProvider = options.ServiceProvider;

                if (serviceProvider == null)
                {
                    var services = options.ServiceCollection ??
                        throw new InvalidOperationException("Service collection was not populated.  HINT: check Program.cs");

                    serviceProvider = services.BuildServiceProvider();

                    options.ServiceProvider = serviceProvider;
                }

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

    /// <summary>
    /// Disposes the service scope for this command. The shared service provider is not
    /// disposed because it belongs to the program rather than to any one command.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_IsDisposed == true)
        {
            return;
        }

        if (disposing == true)
        {
            _ServiceScope?.Dispose();
            _ServiceScope = null;
        }

        _IsDisposed = true;
    }
}