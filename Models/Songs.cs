using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Songs
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public long? AlbumId { get; set; }

    public string? MusicFilePath { get; set; }

    public string? LyricsFilePath { get; set; }

    public string? CoverImagePath { get; set; }

    public string? BannerImagePath { get; set; }

    public string? UploadedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Albums? Album { get; set; }
}
