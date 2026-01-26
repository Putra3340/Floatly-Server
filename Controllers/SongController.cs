using DotNetEnv;
using Floaty_Music.Models;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using TagLib;

namespace Floaty_Music.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SongController : Controller
    {
        private static (DateTime LastUpdate, DashboardStats Stats) _cache;
        private readonly FloatlyContext _context;
        public SongController(FloatlyContext context) { _context = context; }

        [HttpPost]
        public async Task<IActionResult> Upload(SongUploadModel model)
        {
            double musiclength = 0;
            var song = new Songs
            {
                Title = model.Title,
                AlbumId = model.AlbumId,
                MusicFilePath = model.MusicFile != null ? await FileHelper.SaveIFormFileAsync(model.MusicFile, FileHelper.UploadFolder.Music) : null,
                LyricsFilePath = model.LyricsFile != null ? await FileHelper.SaveIFormFileAsync(model.LyricsFile, FileHelper.UploadFolder.Lyrics) : null,
                CoverImagePath = model.CoverImage != null ? await FileHelper.SaveIFormFileAsync(model.CoverImage, FileHelper.UploadFolder.Cover) : null,
                BannerImagePath = model.BannerImage != null ? await FileHelper.SaveIFormFileAsync(model.BannerImage, FileHelper.UploadFolder.Banner) : null,
                MoviePath = model.SpecialMovie != null ? await FileHelper.SaveIFormFileAsync(model.SpecialMovie, FileHelper.UploadFolder.Video) : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            // Extract duration from MP3 file
            //musiclength = await Task.Run(() =>
            //{
            //    using var stream = model.MusicFile.OpenReadStream();
            //    var tagFile = TagLib.File.Create(new StreamFileAbstraction(model.MusicFile.FileName, stream));
            //    return tagFile.Properties.Duration.TotalSeconds;
            //});
            song.SongCounter = new SongCounter[]
            {
                new SongCounter{
                    TotalLikes = 0,
                TotalPlayed = 0,
                MusicLength = (int)musiclength
                }
            };
            await _context.Songs.AddAsync(song);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SongUploadModel model)
        {
            var song = await _context.Songs.FindAsync(model.Id);
            if (song == null) return NotFound();
            song.Title = model.Title;
            song.AlbumId = model.AlbumId;
            double musiclength = 0;
            if (model.MusicFile != null)
            {
                // Extract duration from MP3 file
                using var stream = model.MusicFile.OpenReadStream();
                var tagFile = TagLib.File.Create(new StreamFileAbstraction(model.MusicFile.FileName, stream));
                musiclength = tagFile.Properties.Duration.TotalSeconds;
                song.MusicFilePath = await FileHelper.SaveIFormFileAsync(model.MusicFile, FileHelper.UploadFolder.Music);
            }
            if (model.LyricsFile != null)
                song.LyricsFilePath = await FileHelper.SaveIFormFileAsync(model.LyricsFile, FileHelper.UploadFolder.Lyrics);
            if (model.CoverImage != null)
                song.CoverImagePath = await FileHelper.SaveIFormFileAsync(model.CoverImage, FileHelper.UploadFolder.Cover);
            if (model.BannerImage != null)
                song.BannerImagePath = await FileHelper.SaveIFormFileAsync(model.BannerImage, FileHelper.UploadFolder.Banner);
            if (model.SpecialMovie != null)
                song.MoviePath = await FileHelper.SaveIFormFileAsync(model.SpecialMovie, FileHelper.UploadFolder.Video);
            await _context.SaveChangesAsync();
            var songcounter = await _context.SongCounter.FirstOrDefaultAsync(x => x.SongId == song.Id);
            if (songcounter != null)
            {
                songcounter.MusicLength = (int)musiclength == 0 ? songcounter.MusicLength : (int)musiclength;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            var songcount = await _context.SongCounter.FindAsync(id);
            if (song == null) return NotFound();
            if (songcount == null) return NotFound();
            var playlist = await _context.PlaylistSongs.Where(x => x.SongId == id).ToListAsync();
            if (playlist.Any())
                _context.PlaylistSongs.RemoveRange(playlist);
            _context.SongCounter.Remove(songcount);
            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> CleanUp()
        {
            var songs = _context.Songs.Select(x => new
            {
                MusicFile = GlobalConfiguration.WebRootPath + x.MusicFilePath.Replace("/", "\\"),
                LyricFile = GlobalConfiguration.WebRootPath + x.LyricsFilePath.Replace("/", "\\"),
                CoverFile = GlobalConfiguration.WebRootPath + x.CoverImagePath.Replace("/", "\\"),
                BannerFile = GlobalConfiguration.WebRootPath + x.BannerImagePath.Replace("/", "\\")
            }).ToList();

            var albums = _context.Albums.Select(x => GlobalConfiguration.WebRootPath + x.CoverImagePath.Replace("/", "\\")).ToList();
            var artists = _context.Artists.Select(x => GlobalConfiguration.WebRootPath + x.CoverImagePath.Replace("/", "\\")).ToList();

            var files = Directory.GetFiles(GlobalConfiguration.MusicFilePath).ToList();
            var lyrics = Directory.GetFiles(GlobalConfiguration.LyricsFilePath).ToList();
            var covers = Directory.GetFiles(GlobalConfiguration.CoverImagePath).ToList();
            var banners = Directory.GetFiles(GlobalConfiguration.BannerImagePath).ToList();
            var albumcovers = Directory.GetFiles(GlobalConfiguration.AlbumCoverPath).ToList();
            var artistcovers = Directory.GetFiles(GlobalConfiguration.ArtistProfilePath).ToList();

            var referencedFiles = new HashSet<string>(
    songs.SelectMany(s => new[] { s.MusicFile, s.LyricFile, s.CoverFile, s.BannerFile })
         .Where(p => !string.IsNullOrEmpty(p))
         .Concat(albums.Where(p => !string.IsNullOrEmpty(p)))   // absolute
         .Concat(artists.Where(p => !string.IsNullOrEmpty(p))), // absolute
    StringComparer.OrdinalIgnoreCase
);

            // Now iterate over each category and delete if not referenced
            void DeleteStrayFiles(IEnumerable<string> categoryFiles)
            {
                foreach (var file in categoryFiles)
                {
                    if (!referencedFiles.Contains(file))
                    {
                        try
                        {
                            Console.WriteLine($"Deleting stray file: {file}");
                            System.IO.File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete {file}: {ex.Message}");
                        }
                    }
                }
            }

            // Check all file groups
            DeleteStrayFiles(files);
            DeleteStrayFiles(lyrics);
            DeleteStrayFiles(covers);
            DeleteStrayFiles(banners);
            DeleteStrayFiles(albumcovers);
            DeleteStrayFiles(artistcovers);

            // Refresh dashboard stats
            var tasks = new[] {
                DirectoryScanner.ScanAsync(GlobalConfiguration.MusicFilePath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.LyricsFilePath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.CoverImagePath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.BannerImagePath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.AlbumCoverPath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.ArtistProfilePath)
            };
            var results = await Task.WhenAll(tasks);
            _cache.Stats = new DashboardStats
            {
                Music = new StorageStat { FileCount = results[0].FileCount, TotalSize = results[0].TotalSize },
                Lyrics = new StorageStat { FileCount = results[1].FileCount, TotalSize = results[1].TotalSize },
                Cover = new StorageStat { FileCount = results[2].FileCount, TotalSize = results[2].TotalSize },
                Banner = new StorageStat { FileCount = results[3].FileCount, TotalSize = results[3].TotalSize },
                Album = new StorageStat { FileCount = results[4].FileCount, TotalSize = results[4].TotalSize },
                Artist = new StorageStat { FileCount = results[5].FileCount, TotalSize = results[5].TotalSize }
            };
            _cache.LastUpdate = DateTime.Now;
            return RedirectToAction("DashboardV2");
        }

        public async Task<IActionResult> RefreshDisk()
        {
            // Refresh dashboard stats
            var tasks = new[] {
                DirectoryScanner.ScanAsync(GlobalConfiguration.MusicFilePath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.LyricsFilePath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.CoverImagePath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.BannerImagePath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.AlbumCoverPath),
                DirectoryScanner.ScanAsync(GlobalConfiguration.ArtistProfilePath)
            };

            var results = await Task.WhenAll(tasks);

            _cache.Stats = new DashboardStats
            {
                Music = new StorageStat { FileCount = results[0].FileCount, TotalSize = results[0].TotalSize },
                Lyrics = new StorageStat { FileCount = results[1].FileCount, TotalSize = results[1].TotalSize },
                Cover = new StorageStat { FileCount = results[2].FileCount, TotalSize = results[2].TotalSize },
                Banner = new StorageStat { FileCount = results[3].FileCount, TotalSize = results[3].TotalSize },
                Album = new StorageStat { FileCount = results[4].FileCount, TotalSize = results[4].TotalSize },
                Artist = new StorageStat { FileCount = results[5].FileCount, TotalSize = results[5].TotalSize }
            };
            _cache.LastUpdate = DateTime.Now;

            return RedirectToAction("DashboardV2");
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalSongs = _context.Songs.Count();
            var totalAlbums = _context.Albums.Count();
            var totalArtists = _context.Artists.Count();
            ViewBag.TotalSongs = totalSongs;
            ViewBag.TotalAlbums = totalAlbums;
            ViewBag.TotalArtists = totalArtists;
            ViewBag.TotalPlayed = _context.SongCounter.Sum(x => x.TotalPlayed);
            ViewBag.TotalLikes = _context.SongCounter.Sum(x => x.TotalLikes);
            ViewBag.TopSongs = _context.SongCounter
                .OrderByDescending(x => x.TotalPlayed)
                .Take(5)
                .Include(x => x.Song)
                    .ThenInclude(s => s.Album)
                        .ThenInclude(a => a.Artist)
                .ToList();
            ViewBag.TopSongsLikes = _context.SongCounter
                .OrderByDescending(x => x.TotalLikes)
                .Take(5)
                .Include(x => x.Song)
                    .ThenInclude(s => s.Album)
                        .ThenInclude(a => a.Artist)
                .ToList();
            ViewBag.Albums = _context.Albums.OrderDescending().Include(a => a.Artist).ToList();
            ViewBag.Artists = _context.Artists.OrderDescending().ToList(); // for dropdown
            ViewBag.Songs = _context.Songs
                .OrderByDescending(x => x.Id)
                .Include(x => x.Album)
                    .ThenInclude(a => a.Artist)
                .Include(x => x.SongCounter)
                .ToList();

            // SLOW OPERATION, SO CACHE IT FOR 10 MINUTES
            if (_cache.Stats == null || (DateTime.Now - _cache.LastUpdate).TotalMinutes > 10)
            {
                var tasks = new[] {
                    DirectoryScanner.ScanAsync(GlobalConfiguration.MusicFilePath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.LyricsFilePath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.CoverImagePath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.BannerImagePath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.AlbumCoverPath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.ArtistProfilePath)
                };

                var results = await Task.WhenAll(tasks);

                _cache.Stats = new DashboardStats
                {
                    Music = new StorageStat { FileCount = results[0].FileCount, TotalSize = results[0].TotalSize },
                    Lyrics = new StorageStat { FileCount = results[1].FileCount, TotalSize = results[1].TotalSize },
                    Cover = new StorageStat { FileCount = results[2].FileCount, TotalSize = results[2].TotalSize },
                    Banner = new StorageStat { FileCount = results[3].FileCount, TotalSize = results[3].TotalSize },
                    Album = new StorageStat { FileCount = results[4].FileCount, TotalSize = results[4].TotalSize },
                    Artist = new StorageStat { FileCount = results[5].FileCount, TotalSize = results[5].TotalSize }
                };
                _cache.LastUpdate = DateTime.Now;
            }

            ViewBag.StorageStats = _cache.Stats;
            ViewBag.TotalFiles = _cache.Stats.Music.FileCount
                   + _cache.Stats.Lyrics.FileCount
                   + _cache.Stats.Cover.FileCount
                   + _cache.Stats.Banner.FileCount
                   + _cache.Stats.Album.FileCount
                   + _cache.Stats.Artist.FileCount;

            ViewBag.TotalSize = _cache.Stats.Music.TotalSize
                              + _cache.Stats.Lyrics.TotalSize
                              + _cache.Stats.Cover.TotalSize
                              + _cache.Stats.Banner.TotalSize
                              + _cache.Stats.Album.TotalSize
                              + _cache.Stats.Artist.TotalSize;
            ViewBag.LastUpdate = _cache.LastUpdate;
            return View();
        }
        public async Task<IActionResult> DashboardV2()
        {
            var ctx = _context;

            var vm = new DashboardViewModel();

            // ---- COUNTS ----
            vm.TotalSongs = await ctx.Songs.AsNoTracking().CountAsync();
            vm.TotalAlbums = await ctx.Albums.AsNoTracking().CountAsync();
            vm.TotalArtists = await ctx.Artists.AsNoTracking().CountAsync();

            // ---- TOTAL PLAY / LIKE (single scan) ----
            var totals = await ctx.SongCounter
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalPlayed = g.Sum(x => x.TotalPlayed),
                    TotalLikes = g.Sum(x => x.TotalLikes)
                })
                .FirstOrDefaultAsync();

            vm.TotalPlayed = totals?.TotalPlayed ?? 0;
            vm.TotalLikes = totals?.TotalLikes ?? 0;

            // ---- BASE QUERY ----
            var topSongsBase = ctx.SongCounter
                .AsNoTracking()
                .Where(x => x.UrlId == null)
                .Select(x => new TopSongVm
                {
                    SongId = x.Song.Id,
                    Title = x.Song.Title,
                    Album = x.Song.Album.Title,
                    Artist = x.Song.Album.Artist.Name,
                    TotalPlayed = (long)x.TotalPlayed,
                    TotalLikes = (long)x.TotalLikes
                });

            vm.TopSongs = await topSongsBase
                .OrderByDescending(x => x.TotalPlayed)
                .Take(5)
                .ToListAsync();

            vm.TopSongsLikes = await topSongsBase
                .OrderByDescending(x => x.TotalLikes)
                .Take(5)
                .ToListAsync();

            // ---- ARTISTS ----
            vm.Artists = await ctx.Artists
                .AsNoTracking()
                .Select(a => new ArtistVm
                {
                    Id = a.Id,
                    Name = a.Name,
                    AlbumCount = a.Albums.Count
                })
                .OrderByDescending(a => a.AlbumCount)
                .ToListAsync();


            // SLOW OPERATION, SO CACHE IT FOR 10 MINUTES
            if (_cache.Stats == null || (DateTime.Now - _cache.LastUpdate).TotalMinutes > 10)
            {
                var tasks = new[] {
                    DirectoryScanner.ScanAsync(GlobalConfiguration.MusicFilePath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.LyricsFilePath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.CoverImagePath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.BannerImagePath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.AlbumCoverPath),
                    DirectoryScanner.ScanAsync(GlobalConfiguration.ArtistProfilePath)
                };

                var results = await Task.WhenAll(tasks);

                _cache.Stats = new DashboardStats
                {
                    Music = new StorageStat { FileCount = results[0].FileCount, TotalSize = results[0].TotalSize },
                    Lyrics = new StorageStat { FileCount = results[1].FileCount, TotalSize = results[1].TotalSize },
                    Cover = new StorageStat { FileCount = results[2].FileCount, TotalSize = results[2].TotalSize },
                    Banner = new StorageStat { FileCount = results[3].FileCount, TotalSize = results[3].TotalSize },
                    Album = new StorageStat { FileCount = results[4].FileCount, TotalSize = results[4].TotalSize },
                    Artist = new StorageStat { FileCount = results[5].FileCount, TotalSize = results[5].TotalSize }
                };
                _cache.LastUpdate = DateTime.Now;
            }

            ViewBag.StorageStats = _cache.Stats;
            ViewBag.TotalFiles = _cache.Stats.Music.FileCount
                   + _cache.Stats.Lyrics.FileCount
                   + _cache.Stats.Cover.FileCount
                   + _cache.Stats.Banner.FileCount
                   + _cache.Stats.Album.FileCount
                   + _cache.Stats.Artist.FileCount;

            ViewBag.TotalSize = _cache.Stats.Music.TotalSize
                              + _cache.Stats.Lyrics.TotalSize
                              + _cache.Stats.Cover.TotalSize
                              + _cache.Stats.Banner.TotalSize
                              + _cache.Stats.Album.TotalSize
                              + _cache.Stats.Artist.TotalSize;
            ViewBag.LastUpdate = _cache.LastUpdate;
            return View(vm);
        }
        public class DashboardViewModel
        {
            public int TotalSongs { get; set; }
            public int TotalAlbums { get; set; }
            public int TotalArtists { get; set; }
            public long TotalPlayed { get; set; }
            public long TotalLikes { get; set; }

            public List<TopSongVm> TopSongs { get; set; } = [];
            public List<TopSongVm> TopSongsLikes { get; set; } = [];
            public List<ArtistVm> Artists { get; set; } = [];
        }

        public class TopSongVm
        {
            public int SongId { get; set; }
            public string Title { get; set; }
            public string Album { get; set; }
            public string Artist { get; set; }
            public long TotalPlayed { get; set; }
            public long TotalLikes { get; set; }
        }

        public class ArtistVm
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int AlbumCount { get; set; }
        }


        // For Web API
        [HttpGet]
        public async Task<IActionResult> GetArtist(int start = 0, int end = 10)
        {
            var albums = await _context.Artists.Include(x => x.Albums).ThenInclude(x => x.Songs).Skip(start).Take(end).OrderDescending().Select(x => new { x.Id, x.Name, x.Bio, CoverImagePath = "/uploads/artist/" + x.CoverImagePath, AlbumCount = x.Albums.Count, SongCount = x.Albums.Sum(a => a.Songs.Count) }).ToListAsync();
            return Json(albums);
        }

        [HttpGet]
        public async Task<IActionResult> GetArtistAlbum(int artistid)
        {
            var albums = await _context.Albums.Where(a => a.ArtistId == artistid).OrderDescending().Select(a => new { a.Id, a.Title, a.ReleaseDate, CoverImagePath = "/uploads/album/" + a.CoverImagePath }).ToListAsync();
            return Json(albums);
        }

        [HttpGet]
        public async Task<IActionResult> GetAlbumSong(int albumid)
        {
            var songs = await _context.Songs.Where(s => s.AlbumId == albumid).OrderDescending().Select(s =>
            new
            {
                s.Id,
                s.Title,
                s.AlbumId,
                musicUrl = "/uploads/music/" + s.MusicFilePath,
                coverUrl = "/uploads/cover/" + s.CoverImagePath,
                bannerUrl = "/uploads/banner/" + s.BannerImagePath,
                videoUrl = "/uploads/video/" + s.MoviePath,
                Duration = s.SongCounter.FirstOrDefault().MusicLength,
                Plays = s.SongCounter.FirstOrDefault().TotalPlayed,
                Likes = s.SongCounter.FirstOrDefault().TotalLikes
            }).ToListAsync();
            return Json(songs);
        }

        [HttpGet]
        public async Task<IActionResult> GetLibrarySearch(string query)
        {
            query = query.ToUpper();
            var artist = await _context.Artists.
                Include(x => x.Albums).
                ThenInclude(x => x.Songs).
                ThenInclude(x => x.SongCounter).
                Where(x =>
                    x.Name.ToUpper().Contains(query) || // artist search
                    x.Albums.Any(a => a.Title.ToUpper().Contains(query)) || // album search
                    x.Albums.Any(a => a.Songs.Any(s => s.Title.ToUpper().Contains(query)))). // song search
                OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Bio,
                    CoverImagePath = "/uploads/artist/" + x.CoverImagePath,
                    AlbumCount = x.Albums.Count,
                    SongCount = x.Albums.Sum(a => a.Songs.Count),

                    // Only include albums that match album/song search
                    Albums = x.Albums
            .Where(a =>
                a.Title.ToUpper().Contains(query) ||
                a.Songs.Any(s => s.Title.ToUpper().Contains(query))
            )
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.ReleaseDate,
                CoverImagePath = "/uploads/album/" + a.CoverImagePath,
                SongCount = a.Songs.Count,
                Songs = a.Songs
                    .Where(s => s.Title.ToUpper().Contains(query))
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.SongCounter.FirstOrDefault().MusicLength,
                        s.MusicFilePath,
                        CoverImagePath = "/uploads/cover/" + s.CoverImagePath,
                        //s.Likes,
                        s.SongCounter.FirstOrDefault().TotalLikes
                    })
                    .ToList()
            })
            .ToList()
                })
            .ToListAsync();
            return Json(artist);
        }

        [HttpGet]
        public async Task<IActionResult> CompressAllSongs()
        {
            var songs = await _context.Songs.ToListAsync();
            foreach (var song in songs)
            {
                if (song.MusicFilePath != null && Path.GetExtension(song.MusicFilePath).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    var inputPath = GlobalConfiguration.WebRootPath + song.MusicFilePath.Replace("/", "\\");
                    if (System.IO.File.Exists(inputPath))
                    {
                        try
                        {
                            // we only compress but keep the original file
                            await AudioHelper.CompressAsync(inputPath, "128k");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error compressing {song.Title}: {ex.Message}");
                        }
                    }
                }
            }
            return RedirectToAction("Dashboard");
        }
        #region Youtube
        [HttpGet]
        public async Task<IActionResult> GetYtSong(int start = 1, int end = 10)
        {
            var songs = await _context.YoutubeSongs.Include(x=>x.SongCounter).Skip(start).Take(end).OrderDescending().Select(s =>
            new
            {
                s.Id,
                s.Title,
                s.UrlId,
                s.AuthorName,
                s.AuthorCover,
                musicUrl = "/uploads/yt/" + s.Music,
                coverUrl = "/uploads/yt/" + s.Thumbnail,
                bannerUrl = "/uploads/yt/" + s.AuthorCover,
                videoUrl = "/uploads/yt/" + s.Video,
                Duration = s.SongCounter.FirstOrDefault().MusicLength,
                PlaylistCount = _context.PlaylistSongs.Where(x=>x.UrlId == s.UrlId).Count(),
                Plays = s.SongCounter.FirstOrDefault().TotalPlayed,
                Likes = s.SongCounter.FirstOrDefault().TotalLikes,
                Hidden = s.Hidden
            }).ToListAsync();
            return Json(songs);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteYT(int id)
        {
            var song = await _context.YoutubeSongs.Where(x=>x.Id == id).FirstOrDefaultAsync();
            var songcount = await _context.SongCounter.Where(x=>x.UrlId == song.UrlId).FirstOrDefaultAsync();
            if (song == null) return NotFound();
            if (songcount == null) return NotFound();
            var playlist = await _context.PlaylistSongs.Where(x => x.UrlId == song.UrlId).ToListAsync();
            if (playlist.Any())
                _context.PlaylistSongs.RemoveRange(playlist);
            _context.SongCounter.Remove(songcount);
            var lyrics = await _context.YoutubeLyrics.Where(x => x.SongId == id).ToListAsync();
            if (lyrics.Any())
                _context.YoutubeLyrics.RemoveRange(lyrics);
            _context.YoutubeSongs.Remove(song);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> HideSong([FromBody] HideSongRequest req)
        {
            var song = await _context.YoutubeSongs.Where(x=>x.Id == req.Id).FirstOrDefaultAsync();
            song.Hidden = req.Hidden;
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> GetYtLibrarySearch(string query)
        {
            if (query.IsNullOrEmpty())
            {
                var songs = await _context.YoutubeSongs.Include(x => x.SongCounter).Skip(0).Take(10).OrderDescending().Select(s =>
             new
            {
                s.Id,
                s.Title,
                s.UrlId,
                s.AuthorName,
                s.AuthorCover,
                musicUrl = "/uploads/yt/" + s.Music,
                coverUrl = "/uploads/yt/" + s.Thumbnail,
                bannerUrl = "/uploads/yt/" + s.AuthorCover,
                videoUrl = "/uploads/yt/" + s.Video,
                Duration = s.SongCounter.FirstOrDefault().MusicLength,
                PlaylistCount = _context.PlaylistSongs.Where(x => x.UrlId == s.UrlId).Count(),
                Plays = s.SongCounter.FirstOrDefault().TotalPlayed,
                Likes = s.SongCounter.FirstOrDefault().TotalLikes,
                Hidden = s.Hidden
            }).ToListAsync();
                return Json(songs);
            }
            else
            {

                query = query.ToUpper();
            var songs = await _context.YoutubeSongs
        .Include(x => x.SongCounter)
        .Where(s =>
            s.Title.ToUpper().Contains(query) ||
            s.AuthorName.ToUpper().Contains(query) ||
            s.UrlId.ToUpper().Contains(query)
        ).OrderDescending().Select(s =>
            new
            {
                s.Id,
                s.Title,
                s.UrlId,
                s.AuthorName,
                s.AuthorCover,
                musicUrl = "/uploads/yt/" + s.Music,
                coverUrl = "/uploads/yt/" + s.Thumbnail,
                bannerUrl = "/uploads/yt/" + s.AuthorCover,
                videoUrl = "/uploads/yt/" + s.Video,
                Duration = s.SongCounter.FirstOrDefault().MusicLength,
                PlaylistCount = _context.PlaylistSongs.Where(x => x.UrlId == s.UrlId).Count(),
                Plays = s.SongCounter.FirstOrDefault().TotalPlayed,
                Likes = s.SongCounter.FirstOrDefault().TotalLikes,
                Hidden = s.Hidden
            }).ToListAsync();
            return Json(songs);
            }
        }
        public record HideSongRequest(int Id, bool Hidden);
        #endregion
        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            return Ok(LogCaptureFilter.Logs);
        }
    }
    public class StorageStat
    {
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
    }

    public class DashboardStats
    {
        public StorageStat Music { get; set; }
        public StorageStat Lyrics { get; set; }
        public StorageStat Cover { get; set; }
        public StorageStat Banner { get; set; }
        public StorageStat Album { get; set; }
        public StorageStat Artist { get; set; }
        public int TotalFiles => Music.FileCount + Lyrics.FileCount + Cover.FileCount + Banner.FileCount + Album.FileCount + Artist.FileCount;
        public long TotalSize => Music.TotalSize + Lyrics.TotalSize + Cover.TotalSize + Banner.TotalSize + Album.TotalSize + Artist.TotalSize;
    }

}
