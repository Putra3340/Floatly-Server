using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Floaty_Music.Models;

public partial class FloatlyLibContext : DbContext
{
    public FloatlyLibContext()
    {
    }

    public FloatlyLibContext(DbContextOptions<FloatlyLibContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Library> Library { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-86R216N;Initial Catalog=FloatlyLib;Integrated Security=True;Encrypt=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Library>(entity =>
        {
            entity.Property(e => e.ArtistName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.BannerImagePath)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CoverImagePath)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.LyricsFilePath)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.MusicFilePath)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UploadedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
