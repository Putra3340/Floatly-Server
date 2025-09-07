using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Likes
{
    public int UserId { get; set; }

    public int SongId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Songs Song { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
