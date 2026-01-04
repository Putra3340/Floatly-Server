using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class YoutubeSongs
{
    public long Id { get; set; }

    public string UrlId { get; set; } = null!;

    public string? Title { get; set; }

    public string? Music { get; set; }

    public string? Lyrics { get; set; }

    public string? Thumbnail { get; set; }

    public string? Video { get; set; }

    public string? AuthorName { get; set; }

    public string? AuthorCover { get; set; }

    public bool Hidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PlaylistSongs> PlaylistSongs { get; set; } = new List<PlaylistSongs>();

    public virtual ICollection<SongCounter> SongCounter { get; set; } = new List<SongCounter>();

    public virtual ICollection<YoutubeLyrics> YoutubeLyrics { get; set; } = new List<YoutubeLyrics>();
}
