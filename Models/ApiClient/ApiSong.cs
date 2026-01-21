namespace Floaty_Music.Models.ApiClient
{
    public class ApiSong
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ArtistName { get; set; }
        public string Cover { get; set; }
        public string SongLength { get; set; }
        public string PlayCount { get; set; }
    }
    public class ApiSongPlay
    {
        public string? Id { get => field; set; }
        public string? Title { get => field; set;}
        public string? Music { get => field; set;}
        public string? Lyrics { get => field; set;}
        public string? Cover { get => field; set;}
        public string? Banner { get => field; set;}
        public string? MoviePath { get => field; set;}
        public string? UploadedBy { get => field; set;}
        public string? SongLength { get => field; set;}
        public string? PlayCount { get => field; set;}
        public DateTime CreatedAt { get => field; set;}
        public string? ArtistName { get => field; set;}
        public string? ArtistId { get => field; set;}
        public string? AlbumTitle { get => field; set;}
        public int AlbumId { get => field; set;}
        public bool IsLiked { get => field; set; }
    }
    public class LyricItem
    {
        public string Language { get; set; }
        public string Content { get; set; }
    }

}
