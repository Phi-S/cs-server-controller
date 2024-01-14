using ErrorOr;
using Shared;

namespace Application.Helpers;

public static class FolderHelper
{
    public static string CreateNewTempFolder(string baseFolder)
    {
        var guid = Guid.NewGuid();
        var tempFolderPath = Path.Combine(baseFolder, "temp", guid.ToString());
        Directory.CreateDirectory(tempFolderPath);
        return tempFolderPath;
    }

    public static ErrorOr<Success> CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (dir.Exists == false)
        {
            return Errors.Fail($"Source directory not found: {dir.FullName}");
        }

        Directory.CreateDirectory(destinationDir);

        var dirs = dir.GetDirectories();
        var files = dir.GetFiles();
        
        foreach (var file in files)
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite);
        }
        
        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }

        return Result.Success;
    }
}