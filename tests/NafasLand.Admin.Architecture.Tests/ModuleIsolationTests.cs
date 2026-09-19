using System.Xml.Linq;

namespace NafasLand.Admin.Architecture.Tests;

/// <summary>
/// Tests two ADR-004 / ADR-044 rules:
/// 1) No module has a project reference to another module (only to Shared).
/// 2) Everything outside Contracts in a module is internal.
/// With a single module (Sample), these tests currently find zero violations;
/// but it is the same check that will reject a wrong reference once a second
/// module is added (step 1).
/// </summary>
public sealed class ModuleIsolationTests
{
    private static IReadOnlyList<string> GetModuleProjectFiles()
    {
        var modulesDirectory = Path.Combine(RepositoryPaths.FindRepositoryRoot(), "src", "Modules");
        return Directory.GetDirectories(modulesDirectory)
            .SelectMany(moduleDirectory => Directory.GetFiles(moduleDirectory, "*.csproj", SearchOption.TopDirectoryOnly))
            .ToList();
    }

    [Fact]
    public void هیچ_ماژولی_به_ماژول_دیگر_رفرنس_پروژه‌ای_ندارد()
    {
        var moduleProjectFiles = GetModuleProjectFiles();
        Assert.NotEmpty(moduleProjectFiles);

        foreach (var projectFile in moduleProjectFiles)
        {
            var projectDirectory = Path.GetDirectoryName(projectFile)!;
            var document = XDocument.Load(projectFile);

            var referencedPaths = document
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")!.Value)
                .Select(relativePath => Path.GetFullPath(Path.Combine(projectDirectory, relativePath)));

            foreach (var referencedPath in referencedPaths)
            {
                var referencedDirectory = Path.GetDirectoryName(referencedPath)!;
                var isAnotherModule = referencedDirectory.Contains(Path.Combine("src", "Modules"))
                    && !string.Equals(
                        Path.GetFullPath(referencedDirectory),
                        Path.GetFullPath(projectDirectory),
                        StringComparison.OrdinalIgnoreCase);

                Assert.False(
                    isAnotherModule,
                    $"{Path.GetFileName(projectFile)} نباید رفرنس پروژه‌ای به ماژول دیگر ({referencedPath}) داشته باشد.");
            }
        }
    }

    [Fact]
    public void تمام_نوع‌های_public_هر_ماژول_زیر_namespace_Contracts_هستند()
    {
        var moduleAssemblyNames = GetModuleProjectFiles()
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToList();
        Assert.NotEmpty(moduleAssemblyNames);

        foreach (var assemblyName in moduleAssemblyNames)
        {
            var assembly = System.Reflection.Assembly.Load(assemblyName);
            var publicTypes = assembly.GetExportedTypes();

            foreach (var publicType in publicTypes)
            {
                Assert.True(
                    publicType.Namespace is not null && publicType.Namespace.Contains(".Contracts"),
                    $"{publicType.FullName} در {assemblyName} public است ولی زیر Contracts نیست.");
            }
        }
    }
}
