using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class PlaylistSongs
{
    public long Id { get; set; }

    public int PlaylistId { get; set; }

    public int? SongId { get; set; }

    public string? UrlId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Playlists Playlist { get; set; } = null!;

    public virtual Songs? Song { get; set; }

    public virtual YoutubeSongs? Url { get; set; }
}
