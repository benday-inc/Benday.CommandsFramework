using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace Benday.CommandsFramework;


public abstract class Argument<T>
{
    public Argument(string name, bool isRequired = true, bool allowEmptyValue = true) :
        this(name, name, isRequired, allowEmptyValue)
    {

    }

    public Argument(string name, T value, bool isRequired = true, bool allowEmptyValue = true) :
        this(name, value, name, isRequired, allowEmptyValue)
    {

    }

    public Argument(string name, T value, string description, bool isRequired, bool allowEmptyValue)
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
        Value = value;
        IsRequired = isRequired;
        AllowEmptyValue = allowEmptyValue;
        
        OnInitialize();
    }

    public Argument(string name, string description, bool isRequired, bool allowEmptyValue)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
        }

        if (string.IsNullOrEmpty(description))
        {
            throw new ArgumentException($"'{nameof(description)}' cannot be null or empty.", nameof(description));
        }

        Name = name;
        Description = description;
        IsRequired = isRequired;
        AllowEmptyValue = allowEmptyValue;
        
        _Value = GetDefaultValue();
        
        OnInitialize();
    }

    protected virtual void OnInitialize()
    {

    }

    protected abstract T GetDefaultValue();

    public string Description { get; set; }

    private T _Value;
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
    public bool HasValue { get; private set; }
    public string Name { get; set; }
    public bool IsRequired { get; set; }
    public abstract ArgumentDataType DataType { get; }
    public bool AllowEmptyValue { get; set; }

    public bool Validate()
    {
        if (IsRequired == false)
        {
            return true;
        }
        else
        {
            if (DataType == ArgumentDataType.String)
            {
                var isNullOrWhitespace = string.IsNullOrWhiteSpace(Value as string);

                if (isNullOrWhitespace == true && AllowEmptyValue == true)
                {
                    return true;
                }
                else if (isNullOrWhitespace == true && AllowEmptyValue == false)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else if (DataType == ArgumentDataType.Int32)
            {
                return HasValue;
            }
            else if (DataType == ArgumentDataType.Boolean)
            {
                return HasValue;
            }
            else if (DataType == ArgumentDataType.DateTime)
            {
                return HasValue;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}