namespace Floaty_Music.Models.Modern
{
    public class Lyric
    {
        public string Id { get; set; }
        public List<LyricItem> Lyrics { get; set; } = new List<LyricItem>();
    }
    public class LyricItem
    {
        public string Language { get; set; }
        public string LanguageCode { get; set; }
        public string Text { get; set; } // SRT Formatted Lyrics
    }
}
