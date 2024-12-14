using System;
using System.IO;

namespace MMR.Services;

public class FilePathService
{
    public static string GetUserAvatarUploadsFolderPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        string userUploadsFolderPath = Path.Combine(appDataPath, "MMR", "avatar");
        if (!Directory.Exists(userUploadsFolderPath))
        {
            Directory.CreateDirectory(userUploadsFolderPath);
        }

        return userUploadsFolderPath;
    }
}