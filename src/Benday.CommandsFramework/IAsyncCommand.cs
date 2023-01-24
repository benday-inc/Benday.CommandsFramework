namespace Benday.CommandsFramework;

public interface IAsyncCommand
{
    Task ExecuteAsync();
}
