namespace Floaty_Music.Utils
{
    public static class FileHelper
    {
        public static async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            // 4.59 x 10^-43% Chance for collisions
            var fileName = Path.GetRandomFileName() + Guid.NewGuid() + Path.GetExtension(file.FileName);
            var fullPath = Path.Combine(
                GlobalConfiguration.WebRootPath,
                GlobalConfiguration.UploadsFolder,
                folder,
                fileName);

            // ensure folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
            await file.CopyToAsync(stream);

            return $"/uploads/{Path.GetFileName(folder)}/{fileName}";
        }
        public static async Task<string> SaveIntoFileAsync(string id,string folder,string content)
        {
            var fullPath = Path.Combine(
                GlobalConfiguration.WebRootPath,
                GlobalConfiguration.UploadsFolder,
                folder,
                id);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            File.WriteAllText(fullPath, content);

            return $"/uploads/{Path.GetFileName(folder)}/{id}";
        }
    }


}
