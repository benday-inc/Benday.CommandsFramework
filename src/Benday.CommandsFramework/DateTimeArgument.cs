namespace Benday.CommandsFramework;

public class DateTimeArgument : Argument<DateTime>, IDateTimeArgument
{
    public DateTimeArgument(string name, bool isRequired = true, bool allowEmptyValue = true) :
    base(name, name, isRequired, allowEmptyValue)
    {

    }

    public DateTimeArgument(string name, DateTime value, bool isRequired = true, bool allowEmptyValue = true) :
        base(name, value, name, isRequired, allowEmptyValue)
    {

    }

    public DateTimeArgument(string name, DateTime value, string description, bool isRequired, bool allowEmptyValue) :
        base(name, value, description, isRequired, allowEmptyValue)
    {

    }

    public override ArgumentDataType DataType { get => ArgumentDataType.DateTime; }

    public DateTime ValueAsDateTime => Value;

    protected override DateTime GetDefaultValue()
    {
        return DateTime.MinValue;
    }
}
