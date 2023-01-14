﻿namespace Benday.CommandsFramework;

public class Argument<T>
{
    public Argument(string name, T value, string description, bool isRequired, bool allowEmptyValue)
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
        Value = value;
        IsRequired = isRequired;
        DataType = GetDataTypeFromGenericArgument(typeof(T), DataType);
        AllowEmptyValue = allowEmptyValue;
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
        DataType = GetDataTypeFromGenericArgument(typeof(T), DataType);
        AllowEmptyValue = allowEmptyValue;
    }

    private ArgumentDataType GetDataTypeFromGenericArgument(Type forType, ArgumentDataType dataType)
    {
        if (forType == typeof(string))
        {
            return ArgumentDataType.String;
        }
        else if (forType == typeof(int))
        {
            return ArgumentDataType.Int32;
        }
        else if (forType == typeof(bool))
        {
            return ArgumentDataType.Boolean;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported data type {forType.Name}.");
        }
    }

    public string Description { get; set; }

    private T? _Value;
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
    public ArgumentDataType DataType { get; set; }
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
            else
            { 
                throw new NotImplementedException(); 
            }
        }
    }
}