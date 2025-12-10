namespace Floaty_Music.Utils
{
    public static class FileHelper
    {
        // 4.59 x 10^-43% Chance for collisions
        public static async Task<string> GetRandomFileName() => Path.GetRandomFileName() + Guid.NewGuid();
        public static Task<string> GetPathByEnum(UploadFolder en)
        {
            return Task.FromResult(FolderPaths.TryGetValue(en, out var path) ? path : string.Empty);
        }
        private static readonly Dictionary<UploadFolder, string> FolderPaths = new()
    {
        { UploadFolder.Album, GlobalConfiguration.AlbumCoverPath },
        { UploadFolder.Artist, GlobalConfiguration.ArtistProfilePath },
        { UploadFolder.Banner, GlobalConfiguration.BannerImagePath },
        { UploadFolder.Cover, GlobalConfiguration.CoverImagePath },
        { UploadFolder.Lyrics, GlobalConfiguration.LyricsFilePath },
        { UploadFolder.Music, GlobalConfiguration.MusicFilePath },
        { UploadFolder.Video, GlobalConfiguration.VideoPath },
        { UploadFolder.YT, GlobalConfiguration.YoutubePath },
    };
        public enum UploadFolder
        {
            Album, Artist, Banner, Cover, Lyrics, Music, Video, YT
        }
        public static async Task<string> SaveIFormFileAsync(IFormFile file, UploadFolder folder)
        {
            string outputPath = Path.Combine(await GetPathByEnum(folder), await GetRandomFileName()) + Path.GetExtension(file.FileName);
            using var stream = new FileStream(outputPath, FileMode.Create);
            await file.CopyToAsync(stream);
            return Path.GetFileName(outputPath);
        }

        public static async Task<string> SaveTextAsync(string content , UploadFolder folder)
        {
            string outputPath = Path.Combine(await GetPathByEnum(folder), await GetRandomFileName());
            await File.WriteAllTextAsync(outputPath, content);
            return Path.GetFileName(outputPath);
        }
    }
}
