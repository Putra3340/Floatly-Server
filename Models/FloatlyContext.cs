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

    public virtual DbSet<Playlists> Playlists { get; set; }

    public virtual DbSet<SongCounter> SongCounter { get; set; }

    public virtual DbSet<Songs> Songs { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    public virtual DbSet<VerifiedEmail> VerifiedEmail { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (GlobalConfiguration.isSQLITE)
        {
            optionsBuilder.UseSqlite(GlobalConfiguration.ConnectionString);
        }
        else
        {
            optionsBuilder.UseSqlServer(GlobalConfiguration.ConnectionString);
        }
    }

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
            entity.Property(e => e.ProfileUrl).HasMaxLength(200);
        });

        modelBuilder.Entity<Likes>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.SongId });

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Song).WithMany(p => p.Likes)
                .HasForeignKey(d => d.SongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Likes_Songs");

            entity.HasOne(d => d.User).WithMany(p => p.Likes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Likes_Users");
        });

        modelBuilder.Entity<Playlists>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Playlist__B3016780AC87D4F8");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Playlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Playlists_Users");

            entity.HasMany(d => d.Song).WithMany(p => p.Playlist)
                .UsingEntity<Dictionary<string, object>>(
                    "PlaylistSongs",
                    r => r.HasOne<Songs>().WithMany()
                        .HasForeignKey("SongId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PlaylistSongs_Songs"),
                    l => l.HasOne<Playlists>().WithMany()
                        .HasForeignKey("PlaylistId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PlaylistSongs_Playlists"),
                    j =>
                    {
                        j.HasKey("PlaylistId", "SongId");
                    });
        });

        modelBuilder.Entity<SongCounter>(entity =>
        {
            entity.HasKey(e => e.SongId).HasName("PK_SongCounter_1");

            entity.Property(e => e.SongId)
                .ValueGeneratedNever()
                .HasColumnName("SongID");

            entity.HasOne(d => d.Song).WithOne(p => p.SongCounter)
                .HasForeignKey<SongCounter>(d => d.SongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SongCounter_Songs1");
        });

        modelBuilder.Entity<Songs>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Songs__12E3D6F7F8B323D5");

            entity.Property(e => e.AlbumId).HasColumnName("AlbumID");
            entity.Property(e => e.BannerImagePath).HasMaxLength(255);
            entity.Property(e => e.CoverImagePath).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.LyricsFilePath).HasMaxLength(200);
            entity.Property(e => e.MoviePath).HasMaxLength(255);
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
            entity.HasKey(e => e.Id).HasName("PK__Users__1788CCAC7A3CD2E1");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<VerifiedEmail>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.VerifiedAt).HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
