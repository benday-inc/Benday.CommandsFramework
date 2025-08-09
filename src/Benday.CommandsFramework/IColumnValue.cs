namespace Benday.CommandsFramework
{
    public interface IColumnValue
    {
        string AsString();
        int AsInt();
        float AsFloat();
        bool AsBool();
        double AsDouble();
        decimal AsDecimal();
        object RawValue { get; }
    }
}
