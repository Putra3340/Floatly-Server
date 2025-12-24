using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Songs
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public int? AlbumId { get; set; }

    public string? MusicFilePath { get; set; }

    public string? LyricsFilePath { get; set; }

    public string? CoverImagePath { get; set; }

    public string? BannerImagePath { get; set; }

    public string? MoviePath { get; set; }

    public string? UploadedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Albums? Album { get; set; }

    public virtual ICollection<Likes> Likes { get; set; } = new List<Likes>();

    public virtual ICollection<PlaylistSongs> PlaylistSongs { get; set; } = new List<PlaylistSongs>();

    public virtual ICollection<SongCounter> SongCounter { get; set; } = new List<SongCounter>();
}
