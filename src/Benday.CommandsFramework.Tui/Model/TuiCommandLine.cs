using System.Text;

namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// Builds the command line that the values in a form correspond to.
/// </summary>
/// <remarks>
/// The point of showing it is that the interface teaches the command line: someone finds a
/// command in a form and graduates to typing it. That only works if what is shown is exactly
/// what the tool will accept, so every name here is rendered through ArgumentSyntaxFormatter
/// rather than assembled by hand. A tool that tells people to type something its own parser
/// rejects is worse than one with no help at all.
/// </remarks>
public static class TuiCommandLine
{
    /// <summary>
    /// The command line for a set of fields, as a list of tokens.
    /// </summary>
    /// <param name="commandPath">The command name as it is typed, group included</param>
    /// <param name="fields">The form's fields</param>
    /// <param name="syntax">Which argument syntax the tool accepts</param>
    /// <returns>The tokens, command name first</returns>
    public static List<string> GetTokens(
        string commandPath, IEnumerable<TuiField> fields, ArgumentSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var tokens = new List<string>();

        if (string.IsNullOrWhiteSpace(commandPath) == false)
        {
            tokens.AddRange(commandPath.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        var supplied = fields.Where(IsWorthWriting).ToList();

        // positional values are typed without a name and are read in the order they appear,
        // so they have to come before the named ones and in their declared order
        foreach (var field in supplied
            .Where(x => x.IsPositional == true)
            .OrderBy(x => x.Position))
        {
            tokens.Add(field.Value);
        }

        foreach (var field in supplied.Where(x => x.IsPositional == false))
        {
            // a boolean that is on is typed as a bare flag when the argument allows it, which
            // is the form people write
            if (IsBareFlag(field) == true)
            {
                tokens.Add(syntax.FormatName(field.Name));

                continue;
            }

            tokens.Add(syntax.FormatNameValueAsSingleToken(field.Name, field.Value));
        }

        return tokens;
    }

    /// <summary>
    /// The command line as a single line of text, ready to be typed or copied.
    /// </summary>
    /// <param name="toolName">The name the tool is typed as</param>
    /// <param name="commandPath">The command name as it is typed</param>
    /// <param name="fields">The form's fields</param>
    /// <param name="syntax">Which argument syntax the tool accepts</param>
    /// <returns>The command line</returns>
    public static string GetDisplayText(
        string toolName,
        string commandPath,
        IEnumerable<TuiField> fields,
        ArgumentSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var builder = new StringBuilder();

        builder.Append(Quote(toolName));

        if (string.IsNullOrWhiteSpace(commandPath) == false)
        {
            builder.Append(' ').Append(commandPath);
        }

        var supplied = fields.Where(IsWorthWriting).ToList();

        foreach (var field in supplied
            .Where(x => x.IsPositional == true)
            .OrderBy(x => x.Position))
        {
            builder.Append(' ').Append(Quote(field.Value));
        }

        foreach (var field in supplied.Where(x => x.IsPositional == false))
        {
            builder.Append(' ');

            if (IsBareFlag(field) == true)
            {
                builder.Append(syntax.FormatName(field.Name));

                continue;
            }

            // the spaced form is what a person types, unlike the single token form that
            // exists so a value survives being one element of an argument array
            builder.Append(syntax.FormatNameValue(field.Name, Quote(field.Value)));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Whether a field's value belongs on the command line.
    /// </summary>
    /// <remarks>
    /// A value the user did not supply is not something they would type. A boolean that is
    /// off is the absence of the flag rather than a value, and a value that is only there
    /// because it is the argument's own default would be noise -- leaving it out produces the
    /// same run.
    /// </remarks>
    private static bool IsWorthWriting(TuiField field)
    {
        if (field.Widget == TuiFieldWidget.Toggle)
        {
            return field.IsFlagSet;
        }

        if (field.HasValue == false || string.IsNullOrEmpty(field.Value) == true)
        {
            return false;
        }

        if (field.HasDefaultValue == true &&
            string.Equals(field.Value, field.DefaultValue, StringComparison.Ordinal) == true)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Whether a boolean is typed as a bare flag rather than as a name and a value.
    /// </summary>
    private static bool IsBareFlag(TuiField field)
    {
        return field.Widget == TuiFieldWidget.Toggle &&
            field.IsFlagSet == true &&
            field.Argument.AllowEmptyValue == true;
    }

    /// <summary>
    /// Wraps a value in quotes when it would otherwise be read as more than one token.
    /// </summary>
    private static string Quote(string value)
    {
        value ??= string.Empty;

        if (value.Length == 0)
        {
            return "\"\"";
        }

        return value.Any(char.IsWhiteSpace) == true ? $"\"{value}\"" : value;
    }
}
