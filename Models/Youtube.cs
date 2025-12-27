using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Youtube
{
    public string UrlId { get; set; } = null!;

    public string? Title { get; set; }

    public string? AuthorName { get; set; }

    public int? TotalLikes { get; set; }

    public long? TotalPlayed { get; set; }

    public int? MusicLength { get; set; }

    public string LanguageCode { get; set; } = null!;
}
