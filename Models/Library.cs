using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Library
{
    public long Id { get; set; }

    public string? Title { get; set; }

    public string? ArtistName { get; set; }

    public string? MusicFilePath { get; set; }

    public string? LyricsFilePath { get; set; }

    public string? CoverImagePath { get; set; }

    public string? BannerImagePath { get; set; }

    public string? UploadedBy { get; set; }

    public DateTime? CreatedAt { get; set; }
}
