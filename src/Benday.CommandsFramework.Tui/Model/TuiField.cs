namespace Benday.CommandsFramework.Tui.Model;

/// <summary>
/// One argument as a form field. A view over the real IArgument, not a copy of it, so
/// setting a value here is setting it on the command that will run.
/// </summary>
/// <remarks>
/// This is the payoff of running in process. cmdui reads a JSON mirror of the argument and
/// has to version negotiate with it; here the form holds the argument itself, so validating
/// a field is a direct call and the form can never describe an argument the command does not
/// actually have.
/// </remarks>
public sealed class TuiField
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="argument">The argument this field edits</param>
    public TuiField(IArgument argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        Argument = argument;
        Widget = GetWidget(argument);
        Position = GetPosition(argument);
    }

    /// <summary>
    /// The argument this field edits.
    /// </summary>
    public IArgument Argument { get; }

    /// <summary>
    /// How this field is drawn and edited.
    /// </summary>
    public TuiFieldWidget Widget { get; }

    /// <summary>
    /// The argument's name, which is what goes on the command line.
    /// </summary>
    public string Name => Argument.Name;

    /// <summary>
    /// The label the form shows, which is the friendly name when there is one.
    /// </summary>
    /// <remarks>
    /// FriendlyName is not used by console usage output at all -- it travels in the schema
    /// and becomes a form label, which is exactly what this is.
    /// </remarks>
    public string Label =>
        string.IsNullOrWhiteSpace(Argument.FriendlyName) == true
            ? Argument.Name
            : Argument.FriendlyName;

    /// <summary>
    /// Help text for the field.
    /// </summary>
    public string Description => Argument.Description ?? string.Empty;

    /// <summary>
    /// True when the form should mark this field as one that has to be filled in.
    /// </summary>
    /// <remarks>
    /// An argument that is required but allows an empty value is satisfied by being present
    /// at all, which is how a boolean flag works, so marking it required would be a lie.
    /// </remarks>
    public bool IsRequired => Argument.IsRequired == true && Argument.AllowEmptyValue == false;

    /// <summary>
    /// The values this field will accept, when it is limited to a fixed set.
    /// </summary>
    public IReadOnlyList<string> AllowedValues => Argument.AllowedValues ?? [];

    /// <summary>
    /// True when a default was configured for this argument. The implicit type default does
    /// not count.
    /// </summary>
    public bool HasDefaultValue => Argument.HasDefaultValue;

    /// <summary>
    /// The configured default, empty when there is none.
    /// </summary>
    public string DefaultValue => Argument.DefaultValue ?? string.Empty;

    /// <summary>
    /// True when this argument reads from the tool's stored configuration.
    /// </summary>
    public bool IsFromConfig => Argument.IsFromConfig;

    /// <summary>
    /// True when leaving this blank will make the command go looking for a value.
    /// </summary>
    public bool IsDiscoverable => Argument.IsDiscoverable;

    /// <summary>
    /// What a blank value will make the command search for, when it is discoverable.
    /// </summary>
    public string DiscoveryHint
    {
        get
        {
            if (IsDiscoverable == false)
            {
                return string.Empty;
            }

            var where = string.IsNullOrWhiteSpace(Argument.DiscoveryDirectory) == true
                ? "the current directory"
                : Argument.DiscoveryDirectory;

            var recursively = Argument.DiscoveryIsRecursive == true ? " and below" : string.Empty;

            return $"leave blank to look for '{Argument.DiscoveryPattern}' in {where}{recursively}";
        }
    }

    /// <summary>
    /// True when the argument's value can be typed without naming it.
    /// </summary>
    public bool IsPositional => Argument.IsPositionalSource;

    /// <summary>
    /// Which positional slot this argument reads, or 0 when it is not positional.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// True when a path field's value has to point at something that exists.
    /// </summary>
    public bool MustExist => Argument.MustExist;

    /// <summary>
    /// True when the argument currently holds a value.
    /// </summary>
    public bool HasValue => Argument.HasValue;

    /// <summary>
    /// The argument's current value.
    /// </summary>
    public string Value => Argument.Value ?? string.Empty;

    /// <summary>
    /// True when this field is a boolean that is currently on.
    /// </summary>
    public bool IsFlagSet =>
        Widget == TuiFieldWidget.Toggle &&
        bool.TryParse(Argument.Value, out var flag) == true &&
        flag == true;

    /// <summary>
    /// Tries to put a value into the argument.
    /// </summary>
    /// <remarks>
    /// The argument itself does the converting and only takes the value when it converts, so
    /// a rejected value leaves the previous one alone. That is what makes typing into a
    /// field safe to validate on every keystroke.
    /// </remarks>
    /// <param name="value">What was typed</param>
    /// <returns>Whether the argument took it</returns>
    public bool TrySetValue(string value)
    {
        return Argument.TrySetValue(value ?? string.Empty);
    }

    /// <summary>
    /// Whether the argument is currently valid on its own, ignoring any rule about how it
    /// combines with other arguments.
    /// </summary>
    public bool IsValid()
    {
        return Argument.Validate();
    }

    /// <summary>
    /// What is wrong with the value that was typed, or empty when nothing is.
    /// </summary>
    /// <remarks>
    /// Checked without committing anything: a value the argument refuses is never stored, so
    /// the field keeps whatever it had.
    /// </remarks>
    /// <param name="value">What was typed</param>
    /// <returns>The problem, or empty</returns>
    public string GetProblemWithValue(string value)
    {
        value ??= string.Empty;

        if (string.IsNullOrEmpty(value) == true)
        {
            return IsRequired == true ? $"{Label} is required." : string.Empty;
        }

        if (AllowedValues.Count > 0 &&
            AllowedValues.Contains(value, ArgumentCollection.ArgumentNameComparer) == false)
        {
            return $"{Label} has to be one of: {string.Join(", ", AllowedValues)}.";
        }

        return Widget switch
        {
            TuiFieldWidget.Number when int.TryParse(value, out _) == false =>
                $"{Label} has to be a whole number.",
            TuiFieldWidget.Toggle when bool.TryParse(value, out _) == false =>
                $"{Label} has to be true or false.",
            TuiFieldWidget.Date when DateTime.TryParse(value, out _) == false =>
                $"{Label} has to be a date.",
            TuiFieldWidget.FilePath when MustExist == true && File.Exists(value) == false =>
                $"{Label} has to be a file that exists.",
            TuiFieldWidget.DirectoryPath when MustExist == true && Directory.Exists(value) == false =>
                $"{Label} has to be a directory that exists.",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Works out how a form should draw an argument.
    /// </summary>
    /// <remarks>
    /// AllowedValues is checked before the data type on purpose: a file argument derives from
    /// StringArgument and so can carry a list of allowed values, and a list of choices is
    /// more specific information than "this is a path".
    /// </remarks>
    /// <param name="argument">The argument</param>
    /// <returns>The widget</returns>
    public static TuiFieldWidget GetWidget(IArgument argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (argument.AllowedValues is { Length: > 0 })
        {
            return TuiFieldWidget.Selection;
        }

        if (argument.PathType == ArgumentPathType.File)
        {
            return TuiFieldWidget.FilePath;
        }

        if (argument.PathType == ArgumentPathType.Directory)
        {
            return TuiFieldWidget.DirectoryPath;
        }

        return argument.DataType switch
        {
            ArgumentDataType.Boolean => TuiFieldWidget.Toggle,
            ArgumentDataType.Int32 => TuiFieldWidget.Number,
            ArgumentDataType.DateTime => TuiFieldWidget.Date,
            _ => TuiFieldWidget.Text
        };
    }

    /// <summary>
    /// Reads the positional slot out of the alias a positional argument uses.
    /// </summary>
    /// <remarks>
    /// FromPositionalArgument(n) sets Alias to POSITION_n and binds through the ordinary
    /// alias path, so the position is only recorded there.
    /// </remarks>
    private static int GetPosition(IArgument argument)
    {
        const string Prefix = "POSITION_";

        if (argument.IsPositionalSource == false ||
            argument.Alias is null ||
            argument.Alias.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == false)
        {
            return 0;
        }

        return int.TryParse(argument.Alias[Prefix.Length..], out var position) == true
            ? position
            : 0;
    }

    public override string ToString() => $"{Name} ({Widget})";
}
