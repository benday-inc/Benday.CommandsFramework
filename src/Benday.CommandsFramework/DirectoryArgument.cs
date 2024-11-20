namespace Benday.CommandsFramework;

public class DirectoryArgument : StringArgument
{
    public DirectoryArgument(string name) :
        base(name)
    {
    }

    public override ArgumentDataType DataType { get => ArgumentDataType.String; }
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

public class FileArgument : StringArgument
{
    public FileArgument(string name) :
        base(name)
    {
    }

    public override ArgumentDataType DataType { get => ArgumentDataType.String; }
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
            if (File.Exists(AbsolutePath) == true)
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
