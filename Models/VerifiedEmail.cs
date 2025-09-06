using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class VerifiedEmail
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public DateTime VerifiedAt { get; set; }
}
