using System;
using System.IO;

namespace Floaty_Music.Utils
{
    public static class DirectoryScanner
    {
        public static async Task<(int FileCount, long TotalSize)> ScanAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException($"Directory not found: {path}");

                int fileCount = 0;
                long totalSize = 0;

                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        totalSize += info.Length;
                        fileCount++;
                    }
                    catch
                    {
                        // skip inaccessible files
                    }
                }

                return (fileCount, totalSize);
            });
        }
    }
}
