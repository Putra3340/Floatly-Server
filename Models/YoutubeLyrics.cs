using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class YoutubeLyrics
{
    public int Id { get; set; }

    public long SongId { get; set; }

    public string Language { get; set; } = null!;

    public string LanguageCode { get; set; } = null!;

    public bool IsAuto { get; set; }

    public string FileName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual YoutubeSongs Song { get; set; } = null!;
}
