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

    public virtual DbSet<SongCounter> SongCounter { get; set; }

    public virtual DbSet<Songs> Songs { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    public virtual DbSet<YoutubeLyrics> YoutubeLyrics { get; set; }

    public virtual DbSet<YoutubeSongCounter> YoutubeSongCounter { get; set; }

    public virtual DbSet<YoutubeSongs> YoutubeSongs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=WIN-BNOFJBSA8BF;Initial Catalog=Floatly;Integrated Security=True;Encrypt=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Albums>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Albums__97B4BE17B066FBD5");

            entity.HasIndex(e => e.ArtistId, "IX_Albums_ArtistID");

            entity.Property(e => e.ArtistId).HasColumnName("ArtistID");
            entity.Property(e => e.CoverImagePath).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Artist).WithMany(p => p.Albums)
                .HasForeignKey(d => d.ArtistId)
                .HasConstraintName("FK__Albums__ArtistID__3C69FB99");
        });

        modelBuilder.Entity<Artists>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Artists__25706B700FCDCB2E");

            entity.Property(e => e.Bio).HasColumnType("text");
            entity.Property(e => e.CoverImagePath).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Likes>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.SongId });

            entity.HasIndex(e => e.SongId, "IX_Likes_SongId");

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

        modelBuilder.Entity<PlaylistSongs>(entity =>
        {
            entity.HasKey(e => new { e.PlaylistId, e.SongId });

            entity.HasIndex(e => e.SongId, "IX_PlaylistSongs_SongId");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Playlist).WithMany(p => p.PlaylistSongs)
                .HasForeignKey(d => d.PlaylistId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PlaylistSongs_Playlists");

            entity.HasOne(d => d.Song).WithMany(p => p.PlaylistSongs)
                .HasForeignKey(d => d.SongId)
                .HasConstraintName("FK_PlaylistSongs_Songs");
        });

        modelBuilder.Entity<Playlists>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Playlist__B3016780AC87D4F8");

            entity.HasIndex(e => e.UserId, "IX_Playlists_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Playlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Playlists_Users");
        });

        modelBuilder.Entity<SongCounter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SongCounter_1");

            entity.Property(e => e.UrlId).HasMaxLength(50);

            entity.HasOne(d => d.Song).WithMany(p => p.SongCounter)
                .HasForeignKey(d => d.SongId)
                .HasConstraintName("FK_SongCounter_Songs");

            entity.HasOne(d => d.Url).WithMany(p => p.SongCounter)
                .HasPrincipalKey(p => p.UrlId)
                .HasForeignKey(d => d.UrlId)
                .HasConstraintName("FK_SongCounter_YoutubeSongs");
        });

        modelBuilder.Entity<Songs>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Songs__12E3D6F7F8B323D5");

            entity.HasIndex(e => e.AlbumId, "IX_Songs_AlbumID");

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

        modelBuilder.Entity<YoutubeLyrics>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__YoutubeL__3214EC07039B3CC4");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FileName).HasMaxLength(260);
            entity.Property(e => e.LanguageCode).HasMaxLength(10);

            entity.HasOne(d => d.Song).WithMany(p => p.YoutubeLyrics)
                .HasForeignKey(d => d.SongId)
                .HasConstraintName("FK_YoutubeLyrics_YoutubeSongs");
        });

        modelBuilder.Entity<YoutubeSongCounter>(entity =>
        {
            entity.HasKey(e => e.YtId);
        });

        modelBuilder.Entity<YoutubeSongs>(entity =>
        {
            entity.HasIndex(e => e.UrlId, "IX_YoutubeSongs").IsUnique();

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UrlId).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
