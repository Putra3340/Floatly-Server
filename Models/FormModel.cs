namespace Floaty_Music.Models
{
    public class MusicUploadViewModel
    {
        public string Title { get; set; }
        public string ArtistName { get; set; }
        public IFormFile MusicFile { get; set; }
        public IFormFile LyricsFile { get; set; }
        public IFormFile CoverImage { get; set; }
        public IFormFile BannerImage { get; set; }
    }

}
