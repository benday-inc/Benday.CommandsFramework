namespace Benday.CommandsFramework;

public class CommandInfo
{
    public string Name { get; internal set; } = string.Empty;
    public string Description { get; internal set; } = string.Empty;
    public bool IsAsync { get; internal set; }
    public ArgumentCollection Arguments { get; internal set; } = new ArgumentCollection();
}
