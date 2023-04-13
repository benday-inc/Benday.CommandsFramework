using System.Text;

namespace Benday.CommandsFramework;

public static class LineWrapUtilities
{
    /// <summary>
    /// Appends a wrapped description value for command usages and argument usages
    /// </summary>
    /// <param name="builder">String builder instance to append value to</param>
    /// <param name="valueToWrap">Value to wrap. This is probably the description value.</param>
    /// <param name="wrappedValueMaxLength">Max length of the wrapped value</param>
    /// <param name="commandNameColumnWidth">Arg or command name column width</param>
    public static void AppendWrappedValue(this StringBuilder builder, string valueToWrap,
            int wrappedValueMaxLength, int commandNameColumnWidth)
    {
        var firstLine = valueToWrap.Substring(0, wrappedValueMaxLength);

        var firstLineLastIndexOfSpace = firstLine.LastIndexOf(' ');

        string secondLine = string.Empty;
        string thirdLine = string.Empty;

        if (firstLineLastIndexOfSpace == -1)
        {
            // not sure how to handle a value with no spaces...
            // ...give up and write the unwrapped value
            builder.AppendLine(valueToWrap);
        }
        else
        {
            firstLine = valueToWrap.Substring(0, firstLineLastIndexOfSpace);
            secondLine = valueToWrap[firstLineLastIndexOfSpace..];

            if (secondLine.Length > wrappedValueMaxLength)
            {
                var secondLineTemp = secondLine.Substring(
                    0, wrappedValueMaxLength);

                var secondLineLastIndexOfSpace = secondLineTemp.LastIndexOf(' ');

                thirdLine = secondLine[secondLineLastIndexOfSpace..];

                secondLine = secondLineTemp.Substring(0,
                    secondLineLastIndexOfSpace);
            }

            builder.AppendLine(firstLine.Trim());

            builder.Append(' ', commandNameColumnWidth);
            builder.Append(secondLine.Trim());

            if (string.IsNullOrWhiteSpace(thirdLine) == false)
            {
                builder.AppendLine();
                builder.Append(' ', commandNameColumnWidth);
                builder.Append(thirdLine.Trim());
            }
        }
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
}
