using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Benday.CommandsFramework;

/// <summary>
/// Enumeration of the supported argument data types
/// </summary>
public enum ArgumentDataType
{
    String,
    DateTime,
    Int32,    
    Boolean
}
