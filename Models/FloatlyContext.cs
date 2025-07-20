using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Floaty_Music.Models;

public partial class FloatlyContext : DbContext
{
    public FloatlyContext()
    {
    }

    public FloatlyContext(DbContextOptions<FloatlyContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Albums> Albums { get; set; }

    public virtual DbSet<Artists> Artists { get; set; }

    public virtual DbSet<Likes> Likes { get; set; }

    public virtual DbSet<PlaylistSongs> PlaylistSongs { get; set; }

    public virtual DbSet<Playlists> Playlists { get; set; }

    public virtual DbSet<Songs> Songs { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-86R216N;Initial Catalog=Floatly;Integrated Security=True;Encrypt=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Albums>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Albums__97B4BE17B066FBD5");

            entity.Property(e => e.ArtistId).HasColumnName("ArtistID");
            entity.Property(e => e.CoverUrl).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Artist).WithMany(p => p.Albums)
                .HasForeignKey(d => d.ArtistId)
                .HasConstraintName("FK__Albums__ArtistID__3C69FB99");
        });

        modelBuilder.Entity<Artists>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Artists__25706B700FCDCB2E");

            entity.Property(e => e.Bio).HasColumnType("text");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Likes>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.SongId }).HasName("PK__Likes__76A6F1C310EF151B");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.SongId).HasColumnName("SongID");

            entity.HasOne(d => d.User).WithMany(p => p.Likes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Likes__UserID__49C3F6B7");
        });

        modelBuilder.Entity<PlaylistSongs>(entity =>
        {
            entity.HasKey(e => new { e.PlaylistId, e.SongId }).HasName("PK__Playlist__D22F5AEF4B0329E6");

            entity.Property(e => e.PlaylistId).HasColumnName("PlaylistID");
            entity.Property(e => e.SongId).HasColumnName("SongID");

            entity.HasOne(d => d.Playlist).WithMany(p => p.PlaylistSongs)
                .HasForeignKey(d => d.PlaylistId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PlaylistS__Playl__45F365D3");
        });

        modelBuilder.Entity<Playlists>(entity =>
        {
            entity.HasKey(e => e.PlaylistId).HasName("PK__Playlist__B3016780AC87D4F8");

            entity.Property(e => e.PlaylistId).HasColumnName("PlaylistID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Playlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Playlists__UserI__4222D4EF");
        });

        modelBuilder.Entity<Songs>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Songs__12E3D6F7F8B323D5");

            entity.Property(e => e.AlbumId).HasColumnName("AlbumID");
            entity.Property(e => e.BannerImagePath).HasMaxLength(255);
            entity.Property(e => e.CoverImagePath).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.LyricsFilePath).HasMaxLength(200);
            entity.Property(e => e.MusicFilePath).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.UploadedBy).HasMaxLength(50);

            entity.HasOne(d => d.Album).WithMany(p => p.Songs)
                .HasForeignKey(d => d.AlbumId)
                .HasConstraintName("FK__Songs__AlbumID__3F466844");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC7A3CD2E1");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
