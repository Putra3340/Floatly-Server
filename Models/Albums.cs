using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Albums
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public int? ArtistId { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? CoverUrl { get; set; }

    public virtual Artists? Artist { get; set; }

    public virtual ICollection<Songs> Songs { get; set; } = new List<Songs>();
}
