namespace Floaty_Music.Models
{
    public class SongUploadModel
    {
        public long? Id { get; set; }
        public string Title { get; set; } = null!;
        public long? AlbumId { get; set; }

        public IFormFile? MusicFile { get; set; }
        public IFormFile? LyricsFile { get; set; }
        public IFormFile? CoverImage { get; set; }
        public IFormFile? BannerImage { get; set; }
    }

}
