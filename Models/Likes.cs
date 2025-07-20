using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Likes
{
    public int UserId { get; set; }

    public int SongId { get; set; }

    public virtual Users User { get; set; } = null!;
}
