using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class YoutubeSongCounter
{
    public long YtId { get; set; }

    public int? TotalLikes { get; set; }

    public long? TotalPlayed { get; set; }
}
