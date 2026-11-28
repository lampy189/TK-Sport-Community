using System.Text.Encodings.Web;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests;

// Report 5.1 cases (Dũng): print Method / Input / Expected / Actual like the team's ReportCases so the
// Test Explorer "Standard Output" pane shows the evidence for each UTC row.
public abstract class ReportCaseOutput(ITestOutputHelper output)
{
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    protected static string Show(object? value) => JsonSerializer.Serialize(value, Json);

    protected void Check<T>(string method, string input, T expected, Func<T> run)
    {
        output.WriteLine($"Method: {method}");
        output.WriteLine($"Input: {input}");
        output.WriteLine($"Expected: {Show(expected)}");
        var actual = run();
        output.WriteLine($"Actual: {Show(actual)}");
        Assert.Equal(expected, actual);
    }

    protected async Task CheckAsync<T>(string method, string input, T expected, Func<Task<T>> run)
    {
        output.WriteLine($"Method: {method}");
        output.WriteLine($"Input: {input}");
        output.WriteLine($"Expected: {Show(expected)}");
        var actual = await run();
        output.WriteLine($"Actual: {Show(actual)}");
        Assert.Equal(expected, actual);
    }
}
