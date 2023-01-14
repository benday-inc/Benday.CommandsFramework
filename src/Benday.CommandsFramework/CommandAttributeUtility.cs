using System.Reflection;
using System.Linq;

namespace Benday.CommandsFramework;

public class CommandAttributeUtility
{
    public List<string> GetAvailableCommandNames(Assembly containingAssembly)
    {
        if (containingAssembly is null)
        {
            throw new ArgumentNullException(nameof(containingAssembly));
        }

        var returnValue = new List<string>();

        var matchingTypes =
            from type in containingAssembly.GetTypes()
            where type.GetCustomAttributes<CommandAttribute>().Any()
            select type;

        foreach (var type in matchingTypes)
        {
            var attr = type.GetCustomAttribute<CommandAttribute>();

            if (attr != null)
            {
                returnValue.Add(attr.Name);
            }
        }

        return returnValue;
    }
}
