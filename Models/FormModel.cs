namespace Floaty_Music.Models
{
    public class SongUploadModel
    {
        public int? Id { get; set; }
        public string Title { get; set; } = null!;
        public int? AlbumId { get; set; }

        public IFormFile? MusicFile { get; set; }
        public IFormFile? LyricsFile { get; set; }
        public IFormFile? CoverImage { get; set; }
        public IFormFile? BannerImage { get; set; }
    }

    public class ArtistFormModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Bio { get; set; }
        public IFormFile ProfileUrl { get; set; }
    }

    public class AlbumFormModel
    {
        public int? Id { get; set; }
        public string Title { get; set; } = null!;
        public int ArtistId { get; set; }
        public DateOnly? ReleaseDate { get; set; }
        public IFormFile CoverImage { get; set; }
    }
}
