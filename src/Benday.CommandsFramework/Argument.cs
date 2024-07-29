using System.ComponentModel.DataAnnotations;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

namespace Benday.CommandsFramework;


/// <summary>
/// Base class for arguments
/// </summary>
/// <typeparam name="T">The data type for the argument</typeparam>
public abstract class Argument<T> : IArgument
{
    /// <summary>
    /// Constructor. Create an argument with a value and description.
    /// </summary>
    /// <param name="name">Name of the argument</param>
    /// <param name="value">Value for the argument</param>
    /// <param name="description">Human readable description for the argument</param>
    /// <param name="friendlyName">Human readable namefor the argument</param>
    /// <param name="isRequired">Is this argument required</param>
    /// <param name="allowEmptyValue">Are empty values considered valid?</param>
    public Argument(string name, T value, string description, string friendlyName, bool isRequired, bool allowEmptyValue)
    {
        if (name is null)
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null.", nameof(name));
        }

        if (string.IsNullOrEmpty(description))
        {
            throw new ArgumentException($"'{nameof(description)}' cannot be null or empty.", nameof(description));
        }

        Name = name;
        Description = description;
        FriendlyName = friendlyName;
        Value = value;
        IsRequired = isRequired;
        AllowEmptyValue = allowEmptyValue;

        OnInitialize();
    }

    /// <summary>
    /// Constructor. Create an argument with description but no value.
    /// </summary>
    /// <param name="name">Name of the argument</param>
    /// <param name="description">Human readable for the argument</param>
    /// /// <param name="friendlyName">Human readable namefor the argument</param>
    /// <param name="isRequired">Is this argument required</param>
    /// <param name="allowEmptyValue">Are empty values considered valid?</param>
    public Argument(string name, string description, string friendlyName, bool isRequired, bool allowEmptyValue)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
        }

        if (string.IsNullOrEmpty(description))
        {
            throw new ArgumentException($"'{nameof(description)}' cannot be null or empty.", nameof(description));
        }

        if (string.IsNullOrEmpty(friendlyName) == true)
        {
            FriendlyName = name;
        }
        else
        {
            FriendlyName = friendlyName;
        }

        Name = name;
        Description = description;
        IsRequired = isRequired;
        AllowEmptyValue = allowEmptyValue;

        _Value = GetDefaultValue();

        OnInitialize();
    }

    /// <summary>
    /// Template method for adding logic at the end of the initialization process
    /// </summary>
    protected virtual void OnInitialize()
    {

    }

    /// <summary>
    /// Get the default value for this argument for this data type
    /// </summary>
    /// <returns></returns>
    protected abstract T GetDefaultValue();

    /// <summary>
    /// Human readable description of this argument
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Human readable friendly name for this argument
    /// </summary>
    public string FriendlyName { get; set; }

    private T _Value;

    /// <summary>
    /// The value of this argument
    /// </summary>
    public T Value
    {
        get
        {
            return _Value;
        }
        set
        {
            _Value = value;
            HasValue = true;
        }
    }

    string IArgument.Value { get => Value.ToString(); }

    /// <summary>
    /// Try to set the value for this argument
    /// </summary>
    /// <param name="input">String representation of the argument value</param>
    /// <returns>True if the value could be converted to the argument's data type and the value was set</returns>
    public abstract bool TrySetValue(string input);

    /// <summary>
    /// Does this argument have a value?
    /// </summary>
    public bool HasValue { get; private set; }
    
    /// <summary>
    /// The name of the argument when used on the command line
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The alternate name of the argument when used on the command line
    /// </summary>
    public string Alias { get; set; }

    /// <summary>
    /// Returns true if a command name alias is set
    /// </summary>
    public bool HasAlias
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Alias) == true)
            {
                return false;
            }
            else
            {
                if (Name != Alias)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Should this value come from an unnamed argument on the command line? 
    /// If yes, the Alias value will be the POSITION_x.  
    /// </summary>
    public bool IsPositionalSource { get; set; }

    /// <summary>
    /// Is this argument required to have a value in order to be valid?
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Data type for this argument
    /// </summary>
    public abstract ArgumentDataType DataType { get; }
    
    /// <summary>
    /// Is an empty value valid? This is more helpful for boolean arguments
    /// when the existence of the argument on the command line call 
    /// indicates that something should be done.
    /// </summary>
    public bool AllowEmptyValue { get; set; }

    /// <summary>
    /// Validate this argument according to the configuration.
    /// </summary>
    /// <returns>True if the argument is valid</returns>
    public virtual bool Validate()
    {
        if (IsRequired == false)
        {
            return true;
        }
        else
        {
            return HasValue;
        }
    }
}