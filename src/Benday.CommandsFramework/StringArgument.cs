namespace Benday.CommandsFramework;

public class StringArgument : Argument<string>
{
    public StringArgument(string name, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, name, isRequired, allowEmptyValue)
    {

    }

    public StringArgument(string name, string value, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, value, name, isRequired, allowEmptyValue)
    {

    }

    public StringArgument(string name, string value, string description, bool isRequired, bool allowEmptyValue) :
        base(name, value, description, isRequired, allowEmptyValue)
    {

    }

    /// <summary>
    /// Add a string argument definition with a description
    /// </summary>
    /// <param name="name">Argument name</param>
    /// <param name="noValue">Not used. Only here to distinguish this overload.</param>
    /// <param name="description">Human-friendly description of the argument</param>
    /// <param name="isRequired">Is this a required argument?</param>
    /// <param name="allowEmptyValue">Allow empty values</param>
    public StringArgument(string name, bool noValue, string description, bool isRequired, bool allowEmptyValue) :
        base(name, description, isRequired, allowEmptyValue)
    {
    }

    public override ArgumentDataType DataType { get => ArgumentDataType.String; }
    protected override string GetDefaultValue()
    {
        return string.Empty;
    }

    public override bool Validate()
    {
        if (IsRequired == false)
        {
            return true;
        }
        else
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
    }

    public override bool TrySetValue(string input)
    {
        if (input != null)
        {
            Value = input;
            return true;
        }
        else
        { return false; }
    }
}
