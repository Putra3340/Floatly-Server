using Floaty_Music.Models;
using Floaty_Music.Models.ApiClient;
using Floaty_Music.Service;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Threading.Tasks;
using TagLib.Matroska;
using YoutubeExplode.Common;

namespace Floaty_Music.Controllers
{
    // V3 - Youtube Music Library API
    // BIG TODO : SECURE THIS API
    [Route("api/library/v3")]
    [ApiController]
    public class LibraryV3Controller : ControllerBase
    {
        private readonly FloatlyContext _context;
        private static List<UserRateLimit> PlayCountCooldown = new(); // temp store email verify request
        public LibraryV3Controller(FloatlyContext cont)
        {
            _context = cont;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<ApiSong> combinedsonglist = new();
            List<ApiSong> exsonglist = new();
            var songlist = _context.Songs.Include(x => x.Album).ThenInclude(x => x.Artist).Include(x => x.SongCounter).Take(20).ToList();
            var ytlist = _context.YoutubeSongs.Include(x => x.SongCounter).Take(20).ToList();
            var artistlist = _context.Artists.Take(5).ToList();
            var albumlist = _context.Albums.Include(x => x.Artist).Take(20).ToList();
            var baseUrl = $"{Request.Scheme}://{Request.Host}/";
            foreach (var x in songlist)
            {
                combinedsonglist.Add(new ApiSong
                {
                    Id = x.Id.ToString(),
                    Title = x.Title,
                    ArtistName = x.Album.Artist.Name,
                    Cover = baseUrl + "uploads/cover/" + x.CoverImagePath,
                    SongLength = TimeSpan.FromSeconds((double)x.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                    PlayCount = (x.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                });
                // if the id is integer, add to exsonglist
                if (int.TryParse(x.Id.ToString(), out int parsedId))
                {
                    exsonglist.Add(new ApiSong
                    {
                        Id = x.Id.ToString(),
                        Title = x.Title,
                        ArtistName = x.Album.Artist.Name,
                        Cover = baseUrl + "uploads/cover/" + x.CoverImagePath,
                        SongLength = TimeSpan.FromSeconds((double)x.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                        PlayCount = (x.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                    });
                }
            }
            foreach (var x in ytlist)
            {
                combinedsonglist.Add(new ApiSong
                {
                    Id = x.UrlId.ToString(),
                    Title = x.Title,
                    ArtistName = x.AuthorName,
                    Cover = baseUrl + "uploads/yt/" + x.Thumbnail,
                    SongLength = TimeSpan.FromSeconds((double)x.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                    PlayCount = (x.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                });
            }
            combinedsonglist = combinedsonglist.OrderByDescending(x => int.Parse(x.PlayCount.Split(" Plays")[0])).ToList();
            var result = new
            {
                songs = combinedsonglist,
                songsex = exsonglist,
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
        [HttpGet("search")] // search
        public async Task<IActionResult> Search([FromQuery] string? anycontent, [FromQuery] string? token)
        {
            // dont allow unknown access
            if (!await IsAuthValid(token))
                return Unauthorized();
            if (anycontent.IsNullOrEmpty())
                anycontent = "official music video";
            var list = await YoutubeService.SearchAsync(anycontent, 10);
            List<ApiSong> combinedsonglist = new();
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
            var baseUrl = $"{Request.Scheme}://{Request.Host}/";
            foreach (var x in songlist)
            {
                combinedsonglist.Add(new ApiSong
                {
                    Id = x.Id.ToString(),
                    Title = x.Title,
                    ArtistName = x.Album.Artist.Name,
                    Cover = baseUrl + "uploads/cover/" + x.CoverImagePath,
                    SongLength = TimeSpan.FromSeconds((double)x.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                    PlayCount = (x.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                });
            }
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
        public async Task<IActionResult> GetSong(string id, [FromQuery] string? token)
        {
            // dont auth here - allow public access but for local downloaded only not fetching from youtube
            int localid = 0;
            ApiSongPlay song = null;
            // check is it from youtube / database

            if (!int.TryParse(id, out localid))
            {
                // Youtube
                var songdb = _context.YoutubeSongs.Include(x=>x.SongCounter).FirstOrDefault(x => x.UrlId == id);
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
                        CreatedAt = songdb.CreatedAt,
                        ArtistName = songdb.AuthorName ?? "Unknown Artist",
                        ArtistId = null,
                        AlbumTitle = null,
                        MoviePath = baseUrl + songdb.Video,
                        AlbumId = 0
                    };
                    // increment play count
                    if (songdb?.SongCounter != null && await IsPlayCountNotCooldown(token))
                    {
                        var counter = songdb.SongCounter.FirstOrDefault();
                        if(counter != null)
                        {
                            counter.TotalPlayed += 1;
                            await _context.SaveChangesAsync();
                        }
                    }
                    return Ok(song);
                }
                if (!await IsAuthValid(token)) // dont allow anonymous fetch from youtube
                    return Unauthorized();
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
                var baseUrl = $"{Request.Scheme}://{Request.Host}/uploads/";
                song = new ApiSongPlay()
                {
                    AlbumId = songdb.Album.Id,
                    AlbumTitle = songdb.Album.Title,
                    ArtistId = songdb.Album.Artist.Id.ToString(),
                    ArtistName = songdb.Album.Artist.Name,
                    Cover = $"{baseUrl}/cover/{songdb.CoverImagePath}",
                    Banner = $"{baseUrl}/banner/{songdb.BannerImagePath}",
                    CreatedAt = songdb.CreatedAt,
                    Id = songdb.Id.ToString(),
                    Title = songdb.Title,
                    Lyrics = songdb.LyricsFilePath != null ? $"{baseUrl}/lyrics/{songdb.LyricsFilePath}" : null,
                    Music = songdb.MusicFilePath != null ? $"{baseUrl}/music/{songdb.MusicFilePath}" : null,
                    MoviePath = songdb.MoviePath != null ? $"{baseUrl}/video/{songdb.MoviePath}" : null,
                    UploadedBy = songdb.UploadedBy,
                    SongLength = TimeSpan.FromSeconds((double)songdb.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                    PlayCount = (songdb.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                };
                // increment play count
                if (songdb?.SongCounter != null && await IsPlayCountNotCooldown(token))
                {
                    var counter = songdb.SongCounter.FirstOrDefault();
                    if (counter != null)
                    {
                        counter.TotalPlayed += 1;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok(song);
        }
        [HttpGet("lyrics/{urlId}")]
        public async Task<IActionResult> GetLyrics(string urlId, [FromQuery] string? token)
        {
            if (!await IsAuthValid(token))
                return Unauthorized();
            // local
            int localid = 0;
            if (!int.TryParse(urlId, out localid))
            {
                var song = await _context.YoutubeSongs
                .Where(s => s.UrlId == urlId)
                .Select(s => new
                {
                    s.Id,
                    s.UrlId,
                    Lyrics = s.YoutubeLyrics.Select(l => new
                    {
                        Language = l.Language,
                        LanguageCode = l.LanguageCode,
                        l.IsAuto,
                        l.FileName,
                        Content = System.IO.File.ReadAllText(
                            Path.Combine(GlobalConfiguration.YoutubePath, l.FileName))
                    })
                })
                .FirstOrDefaultAsync();

                if (song == null)
                    return Ok(await YoutubeService.GetLyricsAsync(urlId));
                return Ok(song);
            }
            return NotFound();
        }
        [HttpGet("video/{urlId}")]
        public async Task<IActionResult> GetVideoStream(string urlId, [FromQuery] string? token)
        {
            if (!await IsAuthValid(token))
                return Unauthorized();
            string streamurl = "";
            int localid = 0;
            if (!int.TryParse(urlId, out localid))
            {
                var video = await _context.YoutubeSongs.Where(s => s.UrlId == urlId).FirstOrDefaultAsync();
                if (video != null)
                {
                    streamurl = $"{Request.Scheme}://{Request.Host}/uploads/yt/{video.Video}";
                }
                else { streamurl = await YoutubeService.GetStreamVideoUrl(urlId); }
            }
            else
            {
                var video = await _context.Songs.FindAsync(localid);
                if (video != null)
                {
                    streamurl = $"{Request.Scheme}://{Request.Host}/uploads/video/{video.MoviePath}";
                }
                else
                    return NotFound();
            }
            return Ok(streamurl);
        }
        [HttpGet("hdvideo/{urlId}")]
        public async Task<IActionResult> GetHDVideoStream(string urlId, [FromQuery] string? token)
        {
            if (!await IsAuthValid(token))
                return Unauthorized();
            int localid = 0;
            if (!int.TryParse(urlId, out localid))
            {
                string streamurl = $"{Request.Scheme}://{Request.Host}/uploads/yt/{urlId}_HD.mp4";
            if (System.IO.File.Exists(Path.Combine(GlobalConfiguration.YoutubePath, $"{urlId}_HD.mp4"))){ 
                return Ok(streamurl);
            }
                await YoutubeService.GetHDStreamVideoUrl(urlId);
                return Ok(streamurl);
            }
            var song = _context.Songs.FirstOrDefault(x => x.Id == localid);
            if(song != null)
            {
                string streamurl = $"{Request.Scheme}://{Request.Host}/uploads/video/";
                return Ok(streamurl + song.MoviePath);
            }
            return NotFound();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<bool> IsAuthValid(string token)
        {
            if (token.IsNullOrEmpty())
                return false;
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null)
                return false;
            // check if expired return 
            string exp = string.Empty;
            if (!HashHelper.TryDecodeBase64(user.Token, out exp))
                return false;
            if (exp.IsNullOrEmpty())
                return false;
            exp = exp.Split("|").Last();
            if (long.TryParse(exp, out long epoch))
            {
                var dateTime = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;
                if (dateTime <= DateTime.UtcNow)
                    return false;
            }
            else
            {
                return false;
            }
            return true;
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<bool> IsPlayCountNotCooldown(string token)
        {
            if (token.IsNullOrEmpty())
                return false;
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null)
                return false;
            PlayCountCooldown.RemoveAll(x => x.Expired <= DateTime.Now); // remove expired requests
            // check if expired return 
            var alreadyRequested = PlayCountCooldown.FirstOrDefault(x => x.Token == token && x.Expired > DateTime.Now);
            if (alreadyRequested != default) // on cooldown
                return false;
            PlayCountCooldown.Add(new UserRateLimit
            {
                Token = token,
                Expired = DateTime.Now.AddSeconds(120) // 120 seconds cooldown
            });
            return true;
        }

        public class UserRateLimit
        {
            public string Token { get; set; }
            public DateTime Expired { get; set; } = DateTime.Now;
        }
    }
}
