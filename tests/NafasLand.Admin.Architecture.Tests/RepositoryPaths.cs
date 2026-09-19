namespace NafasLand.Admin.Architecture.Tests;

internal static class RepositoryPaths
{
    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NafasLand.Admin.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("ریشهٔ مخزن (NafasLand.Admin.sln) پیدا نشد.");
    }
}
