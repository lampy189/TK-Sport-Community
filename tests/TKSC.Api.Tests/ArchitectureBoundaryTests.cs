using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class ArchitectureBoundaryTests
{
    private static string Backend
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "be_da", "be_da.csproj")))
                directory = directory.Parent;
            return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), "be_da");
        }
    }

    [Theory]
    [InlineData("Domain", "Application.", "Infrastructure.", "Presentation.", "Microsoft.EntityFrameworkCore", "Microsoft.Data.SqlClient")]
    [InlineData("Application", "Infrastructure.", "Presentation.", "Microsoft.EntityFrameworkCore", "Microsoft.Data.SqlClient", "HttpContext")]
    [InlineData("Infrastructure", "Presentation.", "HttpContext", "IActionResult", "ControllerBase", "Microsoft.AspNetCore.Mvc")]
    [InlineData("Presentation/Controllers", "Infrastructure.", "Interfaces.Repositories", "Microsoft.EntityFrameworkCore", "Microsoft.Data.SqlClient", "DbContext")]
    public void LayersDoNotReferenceForbiddenDependencies(string folder, params string[] forbidden)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(Backend, folder), "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            foreach (var reference in forbidden)
                Assert.False(source.Contains(reference, StringComparison.Ordinal), $"{Path.GetRelativePath(Backend, file)} references {reference}");
        }
    }

    [Fact]
    public void RepositoriesDoNotExposeQueryableAndControllersUseServiceInterfaces()
    {
        var types = typeof(Program).Assembly.GetTypes();
        foreach (var repository in types.Where(t => t.IsInterface && t.Namespace?.EndsWith("Interfaces.Repositories") == true))
            foreach (var method in repository.GetMethods())
                Assert.False(typeof(IQueryable).IsAssignableFrom(method.ReturnType), $"{repository.Name}.{method.Name}");
        foreach (var controller in types.Where(t => typeof(ControllerBase).IsAssignableFrom(t)))
            foreach (var parameter in controller.GetConstructors().SelectMany(c => c.GetParameters()))
                if (parameter.ParameterType.Namespace?.Contains("Application") == true)
                    Assert.True(parameter.ParameterType.IsInterface, $"{controller.Name} injects {parameter.ParameterType.Name}");
    }
}

