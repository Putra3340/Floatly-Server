using Floaty_Music.Models;
using Floaty_Music.Models.ApiClient;
using Floaty_Music.Service;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using TagLib.Matroska;
using YoutubeExplode.Common;

namespace Floaty_Music.Controllers
{
    // V3 - Youtube Music Library API
    [Route("api/library/v3")]
    [ApiController]
    public class LibraryV3Controller : ControllerBase
    {
        private readonly FloatlyContext _context;
        public LibraryV3Controller(FloatlyContext cont)
        {
            _context = cont;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var songlist = _context.Songs
                .Include(s => s.Album).ThenInclude(a => a.Artist)
                .Include(s => s.SongCounter)
                .OrderByDescending(s => s.SongCounter.FirstOrDefault().TotalPlayed) // sort by plays
                .Take(10)
                .ToList();

            var topSongs = new List<ApiSong>();
            foreach (var x in songlist)
            {
                topSongs.Add(new ApiSong
                {
                    Id = x.Id.ToString(),
                    Title = x.Title,
                    ArtistName = x.Album.Artist.Name,
                    Cover = baseUrl + x.CoverImagePath,
                    SongLength = TimeSpan.FromSeconds((double)x.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                    PlayCount = (x.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                });
            }


            var topArtists = _context.Artists
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    coverUrl = baseUrl + a.CoverImagePath,
                    totalPlays = a.Albums
                        .SelectMany(al => al.Songs)
                        .Sum(s => (long?)s.SongCounter.FirstOrDefault().TotalPlayed ?? 0)
                })
                .OrderByDescending(a => a.totalPlays)
                .Take(6)
                .AsEnumerable()
                .Select(a => new
                {
                    a.id,
                    a.name,
                    a.coverUrl,
                    totalPlays = a.totalPlays.ToString("N0") + " Plays"
                })
                .ToList();

            var topAlbums = _context.Albums
                .Select(al => new
                {
                    id = al.Id,
                    title = al.Title,
                    artistName = al.Artist.Name,
                    coverUrl = baseUrl + al.CoverImagePath,
                    totalPlays = al.Songs.Sum(s => (long?)s.SongCounter.FirstOrDefault().TotalPlayed ?? 0)
                })
                .OrderByDescending(al => al.totalPlays)
                .Take(10)
                .AsEnumerable()
                .Select(al => new
                {
                    id = al.id,
                    title = al.title,
                    artistName = al.artistName,
                    coverUrl = al.coverUrl,
                    totalPlays = (al.totalPlays).ToString("N0") + " Plays"
                })
                .ToList();


            var result = new
            {
                songs = topSongs,
                artists = topArtists,
                albums = topAlbums
            };
            return Ok(result);
        }
        [HttpGet("search")] // search
        public async Task<IActionResult> Search([FromQuery] string? anycontent)
        {
            var list = await YoutubeService.SearchAsync(anycontent, 10);
            List<ApiSong> combinedsonglist = new();
            foreach (var x in list)
            {
                combinedsonglist.Add(new ApiSong
                {
                    Id = x.Id,
                    Title = x.Title,
                    ArtistName = x.Author,
                    Cover = x.Thumbnail,
                    SongLength = x.Duration,
                    PlayCount = ""
                });
            }

            List<Songs>? songlist = null;
            List<Artists> artistlist = null;
            List<Albums> albumlist = null;
            if (string.IsNullOrWhiteSpace(anycontent)) // fetch some if empty
            {
                songlist = _context.Songs.Include(x => x.Album).ThenInclude(x => x.Artist).Include(x => x.SongCounter).Take(20).ToList();
                artistlist = _context.Artists.Take(5).ToList();
                albumlist = _context.Albums.Include(x => x.Artist).Take(20).ToList();
            }
            else // filter search
            {
                songlist = _context.Songs.Include(x => x.Album).ThenInclude(x => x.Artist).Include(x => x.SongCounter).Where(x => x.Title.ToUpper().Contains(anycontent.ToUpper())).ToList();
                artistlist = _context.Artists.Where(x => x.Name.ToUpper().Contains(anycontent.ToUpper())).ToList();
                albumlist = _context.Albums.Include(x => x.Artist).Where(x => x.Title.ToUpper().Contains(anycontent.ToUpper())).ToList();
            }
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            foreach (var x in songlist)
            {
                combinedsonglist.Add(new ApiSong
                {
                    Id = x.Id.ToString(),
                    Title = x.Title,
                    ArtistName = x.Album.Artist.Name,
                    Cover = baseUrl + x.CoverImagePath,
                    SongLength = TimeSpan.FromSeconds((double)x.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                    PlayCount = (x.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                });
            }
            var result = new
            {
                songs = combinedsonglist,
                artists = artistlist.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    coverUrl = baseUrl + x.CoverImagePath,
                }).ToList(),
                albums = albumlist.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    artistName = x.Artist.Name,
                    coverUrl = baseUrl + x.CoverImagePath
                }).ToList()
            };
            return Ok(result);
        }

        [HttpGet("play/{id}")]
        public async Task<IActionResult> GetSong(string id)
        {
            int localid = 0;
            ApiSongPlay song = null;
            // check is it from youtube / database

            if (!int.TryParse(id, out localid))
            {
                // Youtube
                // TODO BITRATE
                var songdb = _context.YoutubeSongs.FirstOrDefault(x => x.UrlId == id);
                if (songdb != null)
                {
                    Console.WriteLine("Fetching from Database YT...");
                    var baseUrl = $"{Request.Scheme}://{Request.Host}/uploads/yt/";

                    // check if lyrics empty
                    if (songdb.Lyrics == null || songdb.Lyrics == "")
                        songdb.Lyrics = "empty.srt";
                    // check if there is no lyrics
                    if(!System.IO.File.Exists(Path.Combine(GlobalConfiguration.YoutubePath, songdb.Lyrics)))
                        songdb.Lyrics = "empty.srt";

                    song = new ApiSongPlay()
                    {
                        Id = id,
                        Title = songdb.Title ?? "Unknown Title",
                        Music = baseUrl + songdb.Music,
                        Cover = baseUrl + songdb.Thumbnail,
                        Banner = baseUrl + songdb.Thumbnail,
                        Lyrics = baseUrl + songdb.Lyrics,
                        UploadedBy = songdb.AuthorName ?? "YouTube",
                        SongLength = "Unknown",
                        PlayCount = "",
                        CreatedAt = songdb.CreatedAt ?? DateTime.Now,
                        ArtistName = songdb.AuthorName ?? "Unknown Artist",
                        ArtistId = null,
                        AlbumTitle = null,
                        //MoviePath = baseUrl + songdb.Video, BUGS : FOR SOME REASON IT DIDNT WANT TO PLAY FROM LOCALHOST HTTPS
                        AlbumId = 0
                    };
                    return Ok(song);
                }

                Console.WriteLine("Fetching from YouTube...");
#if DEBUG
                async Task<(T result, TimeSpan time)> Measure<T>(Func<Task<T>> action)
                {
                    var sw = Stopwatch.StartNew();
                    var result = await action();
                    sw.Stop();
                    return (result, sw.Elapsed);
                }
                var streamTask = Measure(() => YoutubeService.StreamAudioAsync(id));
                var videoTask = Measure(() => YoutubeService.GetVideoDetailsAsync(id));
                var lyricsTask = Measure(() => YoutubeService.GetLyrics(id));
#else // RELEASE
                var streamTask = YoutubeService.StreamAudioAsync(id);
                var videoTask = YoutubeService.GetVideoDetailsAsync(id);
                var lyricsTask = YoutubeService.GetLyrics(id);
#endif

                await Task.WhenAll(streamTask, videoTask, lyricsTask);

#if DEBUG
                var (streamurl, streamTime) = await streamTask;
                var (video, videoTime) = await videoTask;
                var (lyrics, lyricsTime) = await lyricsTask;

                Console.WriteLine($"Stream time: {streamTime}");
                Console.WriteLine($"Video time : {videoTime}");
                Console.WriteLine($"Lyrics time: {lyricsTime}");
#else // RELEASE
                var streamurl = await streamTask;
                var video = await videoTask;
                var lyrics = await lyricsTask;
#endif

                string lyricspath = $"{Request.Scheme}://{Request.Host}/empty.srt";
                var priority = new[] { "English", "Indonesia", "Japan", "Korea" };
                var firstlyrics = lyrics
                    .OrderBy(l =>
                    {
                        int idx = Array.IndexOf(priority, l.Language);
                        return idx == -1 ? int.MaxValue : idx; // unknown languages go last
                    })
                    .FirstOrDefault();
                if (firstlyrics != null)
                {
                    string lyricname = await FileHelper.SaveTextAsync($"{id}.srt",firstlyrics.Content, FileHelper.UploadFolder.YT);
                    if(lyricname == null || lyricname == "")
                        lyricspath = $"{Request.Scheme}://{Request.Host}/empty.srt";
                    else
                        lyricspath = $"{Request.Scheme}://{Request.Host}/uploads/yt/" + lyricname;
                    Debug.WriteLine(lyricspath);
                }

                song = new ApiSongPlay()
                {
                    Id = id,
                    Title = video.Title,
                    Music = streamurl,
                    Cover = video.Thumbnails.FirstOrDefault().Url,
                    Banner = video.Thumbnails.GetWithHighestResolution().Url,
                    Lyrics = lyricspath, // give default lyrics
                    UploadedBy = "YouTube",
                    SongLength = video.Duration?.ToString(@"mm\:ss") ?? "Unknown",
                    PlayCount = "",
                    CreatedAt = DateTime.Now,
                    ArtistName = video.Author.ChannelTitle,
                    ArtistId = null,
                    AlbumTitle = null,
                    //MoviePath = 
                    AlbumId = 0
                };

                // Save new song to database async
                _ = Task.Run(async () =>
                {
                    await YoutubeService.DownloadAndSaveAsync(id);
                });
            }
            else
            {
                // Database
                Console.WriteLine("Fetching from Database Local...");
                var songdb = _context.Songs.Include(x => x.SongCounter).Include(x => x.Album).ThenInclude(x => x.Artist).FirstOrDefault(x => x.Id == localid);
                if (songdb == null)
                {
                    return NotFound();
                }
                song = new ApiSongPlay()
                {
                    AlbumId = songdb.Album.Id,
                    AlbumTitle = songdb.Album.Title,
                    ArtistId = songdb.Album.Artist.Id.ToString(),
                    ArtistName = songdb.Album.Artist.Name,
                    Cover = $"{Request.Scheme}://{Request.Host}{songdb.CoverImagePath}",
                    CreatedAt = songdb.CreatedAt ?? DateTime.Now,
                    Id = songdb.Id.ToString(),
                    Title = songdb.Title,
                    Lyrics = songdb.LyricsFilePath != null ? $"{Request.Scheme}://{Request.Host}{songdb.LyricsFilePath}" : null,
                    Music = songdb.MusicFilePath != null ? $"{Request.Scheme}://{Request.Host}{songdb.MusicFilePath}" : null,
                    UploadedBy = songdb.UploadedBy,
                    SongLength = TimeSpan.FromSeconds((double)songdb.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                    PlayCount = (songdb.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                };
            }

            return Ok(song);
        }
        [HttpGet("lyrics/{urlId}")] // TODO : LOCAL DB AND IF NEW SONGS
        public async Task<IActionResult> GetLyrics(string urlId)
        {
            // local
            var song = await _context.YoutubeSongs
                .Where(s => s.UrlId == urlId)
                .Select(s => new
                {
                    s.Id,
                    s.UrlId,
                    Lyrics = s.YoutubeLyrics.Select(l => new
                    {
                        Language = l.LanguageCode,
                        l.IsAuto,
                        l.FileName,
                        Content = System.IO.File.ReadAllText(
                            Path.Combine(GlobalConfiguration.YoutubePath, l.FileName))
                    })
                })
                .FirstOrDefaultAsync();

            if (song == null)
                return NotFound();
            return Ok(song);
        }
        [HttpGet("video/{urlId}")]
        public async Task<IActionResult> GetVideoStream(string urlId)
        {
            string streamurl = await YoutubeService.GetStreamVideoUrl(urlId);
            return Ok(streamurl);
        }
    }
}
