using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class PlaylistSongs
{
    public int PlaylistId { get; set; }

    public int SongId { get; set; }

    public int? OrderIndex { get; set; }

    public virtual Playlists Playlist { get; set; } = null!;
}
