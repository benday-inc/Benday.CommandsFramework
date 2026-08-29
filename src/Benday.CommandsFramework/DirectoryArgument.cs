namespace Benday.CommandsFramework;

/// <summary>
/// Argument implementation for working with paths to directorys.
/// </summary>
/// <remarks>
/// IArgument is named again in the base list on purpose. PathType and MustExist are
/// default interface members, and the interface mapping is established by Argument&lt;T&gt;,
/// which has neither -- so without re-declaring the interface here these members would
/// resolve to the interface defaults and the schema would still report this as a plain
/// string argument.
/// </remarks>
public class DirectoryArgument : StringArgument, IArgument
{
    public DirectoryArgument(string name) :
        base(name)
    {
    }

    public override ArgumentDataType DataType { get => ArgumentDataType.String; }

    /// <summary>
    /// A search pattern that can find this argument's value when it is not supplied. Empty
    /// when the value has to be supplied. Set through DiscoverSingleMatch().
    /// </summary>
    public string DiscoveryPattern { get; set; } = string.Empty;

    /// <summary>
    /// Directory to search. Empty means the working directory.
    /// </summary>
    public string DiscoveryDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Whether the search descends into subdirectories.
    /// </summary>
    public bool DiscoveryIsRecursive { get; set; }

    /// <summary>
    /// This argument's value is a path to a directory.
    /// </summary>
    public ArgumentPathType PathType { get => ArgumentPathType.Directory; }

    protected override string GetDefaultValue()
    {
        return string.Empty;
    }

    /// <summary>
    /// If true, then the directory must exist in order to be considered valid
    /// </summary>
    public bool MustExist { get; set; } = false;

    public override bool Validate()
    {
        var baseIsValid = base.Validate();

        if (baseIsValid == true && MustExist == false)
        {
            return true;
        }
        else if (baseIsValid == true && MustExist == true)
        {
            if (Directory.Exists(AbsolutePath) == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public string AbsolutePath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Value) == true)
            {
                return string.Empty;
            }
            else
            {
                var temp = CommandFrameworkUtilities.GetFullyQualifiedPath(Value);

                return temp;
            }
        }
    }
}
