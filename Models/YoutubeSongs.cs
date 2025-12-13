using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class YoutubeSongs
{
    public long Id { get; set; }

    public string? UrlId { get; set; }

    public string? Music { get; set; }

    public string? Lyrics { get; set; }

    public string? Thumbnail { get; set; }

    public string? Video { get; set; }

    public string? AuthorName { get; set; }

    public string? AuthorCover { get; set; }

    public DateTime? CreatedAt { get; set; }
}
