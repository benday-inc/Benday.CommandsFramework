namespace Benday.CommandsFramework;

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
}
