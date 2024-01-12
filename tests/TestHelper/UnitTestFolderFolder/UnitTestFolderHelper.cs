using Xunit.Abstractions;

namespace TestHelper.UnitTestFolderFolder;

public static class UnitTestFolderHelper
{
    public static string GetNewUnitTestFolder(ITestOutputHelper outputHelper)
    {
        var guid = Guid.NewGuid();
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "cs-controller-instance-unit-tests", guid.ToString());
        Directory.CreateDirectory(folder);
        outputHelper.WriteLine($"Test folder: {folder}");
        return folder;
    }
}