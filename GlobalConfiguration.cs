namespace Floaty_Music
{
    public static class GlobalConfiguration
    {
        public static string ConnectionString { get; set; } = "Data Source=DESKTOP-86R216N;Initial Catalog=FloatlyLib;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
        public static string WebRootPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        public static string UploadsFolder { get; set; } = "uploads";
        public static string MusicFilePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "music");
        public static string LyricsFilePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "lyrics");
        public static string CoverImagePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "cover");
        public static string BannerImagePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "banner");


        public static string SaveFile(IFormFile file, string folder)
        {
            if (file == null) return null;
            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var relativePath = Path.Combine("uploads", folder, fileName).Replace("\\", "/");
            var absolutePath = Path.Combine(WebRootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            using var stream = new FileStream(absolutePath, FileMode.Create);
            file.CopyTo(stream);
            return "/" + relativePath;
        }
    }
}
