using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class SongCounter
{
    public int Id { get; set; }

    public int? SongId { get; set; }

    public int? TotalLikes { get; set; }

    public long? TotalPlayed { get; set; }

    public int? MusicLength { get; set; }

    public virtual Songs? Song { get; set; }
}
