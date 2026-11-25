using Xunit;

namespace TKSC.Api.Tests.Stock;

// One unit test case of Report 5.1. The Report 5.1 generator (kept outside this repository) reads these
// arguments from source and the result from the .trx file, so the sheet always matches the code.
[AttributeUsage(AttributeTargets.Method)]
public sealed class UnitCaseAttribute : FactAttribute
{
    public UnitCaseAttribute(string id, string function, string type, string input, string expected, string pre = "N/A")
    {
        DisplayName = $"{id} {function} [{type}]";
        _ = (input, expected, pre);
    }
}
