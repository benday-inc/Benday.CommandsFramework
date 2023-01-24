namespace Benday.CommandsFramework;

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public bool IsAsync { get; set; } = false;
    public string Description { get; set; } = string.Empty;
}
