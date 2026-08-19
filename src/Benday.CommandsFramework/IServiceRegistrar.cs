using Microsoft.Extensions.DependencyInjection;

namespace Benday.CommandsFramework;

/// <summary>
/// Implement this on a class in an assembly of commands to register the services those
/// commands need, without Program.cs having to know about them.
/// </summary>
/// <remarks>
/// This is the answer to "an assembly of commands should be able to declare its own
/// dependencies", and it is deliberately a <b>startup</b> hook rather than a per-command one.
/// Microsoft.Extensions.DependencyInjection seals its registrations when the provider is
/// built, and the provider is built once and cached so that singletons really are singletons
/// -- so a registration hook on the command would compile, run, and silently do nothing.
///
/// Registrars are discovered by the same assembly scan that finds commands and are invoked
/// by CommandsApp before the provider is built. The implementing class needs a public
/// parameterless constructor.
/// </remarks>
public interface IServiceRegistrar
{
    /// <summary>
    /// Add this assembly's services to the collection.
    /// </summary>
    /// <param name="services">Service collection to add to</param>
    void Register(IServiceCollection services);
}
