using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Artists
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Bio { get; set; }

    public string? CoverImagePath { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Albums> Albums { get; set; } = new List<Albums>();
}
