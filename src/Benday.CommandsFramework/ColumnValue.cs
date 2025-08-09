using System;
using System.Globalization;

namespace Benday.CommandsFramework
{
    public class ColumnValue : IColumnValue
    {
        private readonly string _value;

        public ColumnValue(string value)
        {
            _value = value;
        }

        public string AsString() => _value;

        public int AsInt() => int.TryParse(_value, out var result) ? result : throw new FormatException($"Cannot convert '{_value}' to int.");

        public float AsFloat() => float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : throw new FormatException($"Cannot convert '{_value}' to float.");

        public bool AsBool() => bool.TryParse(_value, out var result) ? result : throw new FormatException($"Cannot convert '{_value}' to bool.");

        public double AsDouble() => double.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : throw new FormatException($"Cannot convert '{_value}' to double.");

        public decimal AsDecimal() => decimal.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : throw new FormatException($"Cannot convert '{_value}' to decimal.");

        public object RawValue => _value;
    }
}
