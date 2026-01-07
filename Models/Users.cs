using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Users
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime PremiumExpired { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? Token { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Playlists> Playlists { get; set; } = new List<Playlists>();

    public virtual ICollection<Transaction> Transaction { get; set; } = new List<Transaction>();
}
