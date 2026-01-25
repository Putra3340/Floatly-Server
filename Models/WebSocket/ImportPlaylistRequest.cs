namespace Floaty_Music.Models.WebSocket
{
    public class ImportPlaylistRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
