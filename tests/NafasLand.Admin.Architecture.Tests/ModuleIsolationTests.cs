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

    /// <summary>
    /// Catalog writes ProductRef and parent-entity audit data through Kernel
    /// contracts (IProductRefWriter, IAuditContext) only — the project-reference
    /// check above would already catch a csproj reference; this also catches a
    /// compiled-in dependency on the Auditing assembly.
    /// </summary>
    [Fact]
    public void Catalog_هیچ_وابستگی_اسمبلی_به_Auditing_ندارد()
    {
        var catalog = System.Reflection.Assembly.Load("NafasLand.Admin.Modules.Catalog");

        Assert.DoesNotContain(
            catalog.GetReferencedAssemblies(),
            reference => reference.Name == "NafasLand.Admin.Modules.Auditing");
    }

    /// <summary>
    /// ADR-054: Returns reaches Approvals only through the keyed
    /// IApprovalExecutor and the portal only through Shared.Infrastructure's
    /// shared client — no compiled-in dependency on any other module.
    /// </summary>
    [Fact]
    public void Returns_به_هیچ_ماژول_دیگری_وابستگی_اسمبلی_ندارد()
    {
        var returns = System.Reflection.Assembly.Load("NafasLand.Admin.Modules.Returns");

        Assert.DoesNotContain(
            returns.GetReferencedAssemblies(),
            reference => reference.Name!.StartsWith("NafasLand.Admin.Modules.", StringComparison.Ordinal));
    }
}
