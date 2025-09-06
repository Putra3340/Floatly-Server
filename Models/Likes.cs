using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Likes
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? SongList { get; set; }

    public virtual Users User { get; set; } = null!;
}
