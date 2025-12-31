using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Playlists
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public bool SpecialPlaylist { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PlaylistSongs> PlaylistSongs { get; set; } = new List<PlaylistSongs>();

    public virtual Users User { get; set; } = null!;
}
