using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Users
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Likes> Likes { get; set; } = new List<Likes>();

    public virtual ICollection<Playlists> Playlists { get; set; } = new List<Playlists>();
}
