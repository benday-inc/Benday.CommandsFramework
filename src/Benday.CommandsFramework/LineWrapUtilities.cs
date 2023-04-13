using System.Text;

namespace Benday.CommandsFramework;

public static class LineWrapUtilities
{
    /// <summary>
    /// Appends a wrapped description value for command usages and argument usages
    /// </summary>
    /// <param name="builder">String builder instance to append value to</param>
    /// <param name="valueToWrap">Value to wrap. This is probably the description value.</param>
    /// <param name="maxLineLength">Max length of the line</param>
    /// <param name="commandNameColumnWidth">Arg or command name column width</param>
    public static void AppendWrappedValue(this StringBuilder builder, string valueToWrap,
            int maxLineLength, int commandNameColumnWidth)
    {
        builder.Append(WrapValue(commandNameColumnWidth, maxLineLength, valueToWrap));
    }

    /// <summary>
    /// Appends a value to the string builder with trailing space padding
    /// </summary>
    /// <param name="builder">String builder to append to</param>
    /// <param name="value">Value to append</param>
    /// <param name="padToLength">Length to make the value</param>
    public static void AppendWithPadding(this StringBuilder builder,
        string value, int padToLength)
    {
        builder.Append(value);
        builder.Append(' ', padToLength - value.Length);
    }

    /// <summary>
    /// Gets a string value with trailing space padding
    /// </summary>
    /// <param name="value">Value to pad</param>
    /// <param name="padToLength">Desired length after padding</param>
    /// <returns></returns>
    public static string GetValueWithPadding(string value, int padToLength)
    {
        var builder = new StringBuilder();

        builder.Append(value);
        builder.Append(' ', padToLength - value.Length);

        return builder.ToString();
    }

    public static string WrapValue(
        int linePadding, int maxLineLength, string input)
    {
        int maxColumnLength = (maxLineLength - linePadding);

        if (input.Length <= maxColumnLength)
        {
            return input;
        }
        else
        {
            var lines = new List<string>();

            var words = input.Split(' ');

            var lineBuilder = new StringBuilder();

            foreach (var word in words)
            {
                if (lineBuilder.Length + (word.Length + 1) <= maxColumnLength)
                {
                    // it fits on the line...add it
                    lineBuilder.Append(' ');
                    lineBuilder.Append(word);
                }
                else
                {
                    lines.Add(lineBuilder.ToString());

                    lineBuilder = new StringBuilder();

                    lineBuilder.Append(' ');
                    lineBuilder.Append(word);
                }
            }

            if (lineBuilder.Length > 0)
            {
                lines.Add(lineBuilder.ToString());
            }

            var returnValueBuilder = new StringBuilder();

            bool isFirst = true;

            foreach (var line in lines)
            {
                if (isFirst == true)
                {
                    returnValueBuilder.Append(line.Trim());
                    isFirst = false;
                }
                else
                {
                    returnValueBuilder.AppendLine();
                    returnValueBuilder.Append(' ', linePadding);
                    returnValueBuilder.Append(line.Trim());
                }
            }

            return returnValueBuilder.ToString();
        }
    }
}
