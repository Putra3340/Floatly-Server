using Floaty_Music.Models;
using Floaty_Music.Models.ApiClient;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using TagLib.Mpeg;
using Xabe.FFmpeg;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Floaty_Music.Service
{
    public static class YoutubeService
    {
        private static readonly YoutubeClient client = new YoutubeClient();
        private static readonly FloatlyContext db = new FloatlyContext();

        // Pending Download
        private static readonly HashSet<string> pending = new();
        private static readonly object _lock = new();

        // Always use this, but make it fire and forget
        public static async Task DownloadAndSaveAsync(string youtubeUrl)
        {
            // if there is pending abort
            lock (_lock)
            {
                if (!pending.Add(youtubeUrl))
                {
                    Console.WriteLine("Task already running: " + youtubeUrl);
                    return;
                }
            }
            var client = new YoutubeClient();
            var videoId = VideoId.Parse(youtubeUrl);

            Directory.CreateDirectory(GlobalConfiguration.YoutubePath);
            var baseName = videoId.Value;

            // filenames only (what goes to DB)
            var audioFile = baseName + ".m4a";
            var videoFile = baseName + ".mp4";
            var thumbFile = baseName + ".jpg";

            // full disk paths
            var audioPath = Path.Combine(GlobalConfiguration.YoutubePath, audioFile);
            var videoPath = Path.Combine(GlobalConfiguration.YoutubePath, videoFile);
            var thumbPath = Path.Combine(GlobalConfiguration.YoutubePath, thumbFile);

            var manifest = await client.Videos.Streams.GetManifestAsync(videoId);


            // check if video has higher than 30 minutes duration
            var videoInfo = await client.Videos.GetAsync(videoId);
            if (videoInfo.Duration.HasValue && videoInfo.Duration.Value.TotalMinutes > 30)
            {
                Console.WriteLine($"Video duration exceeds 30 minutes. Skipping download: {youtubeUrl}");
                lock (_lock)
                    pending.Remove(youtubeUrl);
                return;
            }
            // AUDIO
            var audio = manifest.GetAudioOnlyStreams()
                                .GetWithHighestBitrate()
                ?? throw new Exception("No audio stream found.");

            await client.Videos.Streams.DownloadAsync(audio, audioPath);

            // VIDEO - we take the low res because it's just for visualizer, but for HD is manually converted
            var video = manifest.GetVideoStreams()
                                .FirstOrDefault()
                ?? throw new Exception("No video stream found.");

            await client.Videos.Streams.DownloadAsync(video, videoPath);

            // THUMBNAIL + AUTHOR
            var info = await client.Videos.GetAsync(videoId);
            var thumbUrl = info.Thumbnails.GetWithHighestResolution().Url;

            using (var http = new HttpClient())
            {
                var bytes = await http.GetByteArrayAsync(thumbUrl);
                await System.IO.File.WriteAllBytesAsync(thumbPath, bytes);
            }


            // SAVE TO DB TRX
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // Save Song
                var dbSong = new YoutubeSongs
                {
                    Title = info.Title,
                    UrlId = videoId.Value,
                    Music = audioFile,
                    Video = videoFile,
                    Lyrics = youtubeUrl + ".srt",
                    Thumbnail = thumbFile,
                    AuthorName = info.Author.ChannelTitle,
                    AuthorCover = null,
                    CreatedAt = DateTime.UtcNow
                };

                await db.YoutubeSongs.AddAsync(dbSong);

                var captionsManifest = await client.Videos.ClosedCaptions.GetManifestAsync(videoId);

                foreach (var track in captionsManifest.Tracks)
                {
                    var captions = await client.Videos.ClosedCaptions.GetAsync(track);

                    var sb = new StringBuilder();
                    int i = 1;

                    foreach (var c in captions.Captions)
                    {
                        sb.AppendLine(i.ToString());
                        sb.AppendLine(
                            $"{c.Offset:hh\\:mm\\:ss\\,fff} --> {(c.Offset + c.Duration):hh\\:mm\\:ss\\,fff}"
                        );
                        sb.AppendLine(c.Text);
                        sb.AppendLine();
                        i++;
                    }

                    var lang = track.Language.Code ?? "und";
                    var isAuto = track.IsAutoGenerated || lang == "und";

                    var fileName = isAuto
                        ? $"{baseName}_auto.srt"
                        : $"{baseName}_{lang}.srt";

                    var fullPath = Path.Combine(GlobalConfiguration.YoutubePath, fileName);
                    await System.IO.File.WriteAllTextAsync(fullPath, sb.ToString());

                    db.YoutubeLyrics.Add(new YoutubeLyrics
                    {
                        Song = dbSong,
                        Language = track.Language.Name,
                        LanguageCode = lang,
                        IsAuto = isAuto,
                        FileName = fileName
                    });
                }

                // Add Counter
                var songCounter = new SongCounter
                {
                    Url = dbSong,
                    TotalPlayed = 1,
                    TotalLikes = 0,
                    MusicLength = (int?)(info.Duration.GetValueOrDefault().TotalSeconds) ?? 0
                };
                await db.SongCounter.AddAsync(songCounter);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            } catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error downloading {youtubeUrl}: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                    pending.Remove(youtubeUrl);
            }
            Console.WriteLine($"Succesfully downloaded {youtubeUrl} and saved to db");
        }

        public static async Task<string> StreamAudioAsync(string youtubeUrl)
        {
            var decodedUrl = Uri.UnescapeDataString(youtubeUrl);
            var videoId = VideoId.Parse(decodedUrl);

            var manifest = await client.Videos.Streams.GetManifestAsync(videoId);

            var audio = manifest.GetAudioOnlyStreams()
                                .OrderByDescending(s => s.Bitrate)
                                .FirstOrDefault();

            if (audio == null)
                throw new Exception("No audio streams found.");

            return audio.Url;
        }

        // ============================
        // GET ALL LYRICS (ALL SUBS)
        // ============================
        public static async Task<List<string>> GetLyricsAsync(string youtubeUrl)
        {
            var videoId = VideoId.Parse(youtubeUrl);
            var manifest = await client.Videos.ClosedCaptions.GetManifestAsync(videoId);

            var result = new List<string>();

            foreach (var track in manifest.Tracks)
            {
                var captions = await client.Videos.ClosedCaptions.GetAsync(track);

                var text = string.Join("\n", track.Language.Name);
                result.Add(text);
            }

            return result;
        }

        // USE THIS
        public static async Task<List<YoutubeSearchResult>> SearchAsync(string query = "official music video", int count = 5)
        {
#if DEBUG
            Stopwatch sw = new();
            Console.WriteLine($"Start fetching : {query}");
            sw.Start();
#endif
            var results = new List<YoutubeSearchResult>();
            int trycount = 1;
        retry:
            try
            {
                // 20 January 2025 - dont limit the duration just make if higher than 30 minutes dont cache/download it
                var search = client.Search.GetVideosAsync(query).Where(x=>x.Duration >= TimeSpan.FromSeconds(30));

                await foreach (var video in search.Take(count))
                {
                    results.Add(new YoutubeSearchResult
                    {
                        Id = video.Id.Value,
                        Url = $"https://youtu.be/{video.Id.Value}",
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        Duration = video.Duration is TimeSpan d
                                    ? (d.Hours > 0
                                        ? d.ToString(@"hh\:mm\:ss")
                                        : d.ToString(@"mm\:ss"))
                                    : "Unknown",
                        Thumbnail = video.Thumbnails.GetWithHighestResolution().Url
                    });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("known"))
                {
                    if (trycount <= 3)
                    {
                        trycount++;
                        Console.WriteLine("Trying again, Try Count : " + trycount);
                        goto retry;
                    }
                }
            }
#if DEBUG
            sw.Stop();
            Console.WriteLine($"Elapsed time: {sw.Elapsed}");
#endif
            return results;
        }
        public static async Task<string> GetStreamVideoUrl(string yturl)
        {
#if DEBUG
            Stopwatch sw = new();
            Console.WriteLine($"Start fetching : {yturl}");
            sw.Start();
#endif
            var videoId = VideoId.Parse(yturl);
            var video = await client.Videos.Streams.GetManifestAsync(videoId);
            var videostream = video.GetVideoStreams().FirstOrDefault();
            if (videostream == null)
                throw new Exception("No video streams found.");
#if DEBUG
            sw.Stop();
            Console.WriteLine($"Elapsed time: {sw.Elapsed}");
#endif
            return videostream.Url;
        }
        public static async Task<string> GetHDStreamVideoUrl(string yturl)
        {
            var audioFile = yturl + "_temp.m4a";
            var videoFile = yturl + "_temp.mp4";
            var audioPath = Path.Combine(GlobalConfiguration.YoutubePath, audioFile);
            var videoPath = Path.Combine(GlobalConfiguration.YoutubePath, videoFile);
#if DEBUG
            Stopwatch sw = new();
            Console.WriteLine($"Start fetching : {yturl}");
            sw.Start();
#endif
            var videoId = VideoId.Parse(yturl);
            var manifest = await client.Videos.Streams.GetManifestAsync(videoId);
            var videoStreams = manifest.GetVideoStreams();

            var videoStream =
    videoStreams.FirstOrDefault(v => v.VideoQuality.Label == "720p60")
    ?? videoStreams.FirstOrDefault(v => v.VideoQuality.Label == "720p")
    ?? videoStreams
        .OrderByDescending(v => v.VideoQuality.MaxHeight)
        .FirstOrDefault();

            if (videoStream == null)
                throw new Exception("No video streams found.");


            await client.Videos.Streams.DownloadAsync(videoStream, videoPath);
            // AUDIO
            var audio = manifest.GetAudioOnlyStreams()
                                .GetWithHighestBitrate()
                ?? throw new Exception("No audio stream found.");

            await client.Videos.Streams.DownloadAsync(audio, audioPath);

#if DEBUG
            sw.Stop();
            Console.WriteLine($"Elapsed time: {sw.Elapsed}");
#endif
            await FFmpegHelper.MuxAsync(videoPath, audioPath, Path.Combine(GlobalConfiguration.YoutubePath, $"{yturl}_HD.mp4"));
            return Path.Combine(GlobalConfiguration.YoutubePath, $"{yturl}_HD.mp4");
        }

        // Get Details from URL
        public static async Task<Video> GetVideoDetailsAsync(string youtubeUrl)
        {
            var videoId = VideoId.Parse(youtubeUrl);
            var video = await client.Videos.GetAsync(videoId);
            return video;
        }
        public static async Task<List<LyricItem>> GetLyrics(string yturl)
        {
            var videoId = VideoId.Parse(yturl);
            var manifest = await client.Videos.ClosedCaptions.GetManifestAsync(videoId);

            var result = new List<LyricItem>();

            foreach (var track in manifest.Tracks)
            {
                var captions = await client.Videos.ClosedCaptions.GetAsync(track);


                // 29 NOVEMBER Parse Youtube Caption as SRT
                // Credits by Putra3340
                string text = string.Empty;

                int i = 1;
                foreach (var item in captions.Captions)
                {
                    #if DEBUG
                    Debug.WriteLine(i);
                    Debug.WriteLine($"{item.Offset} --> {item.Offset.Add(item.Duration)}");
                    Debug.WriteLine(item.Text);
                    #endif
                    text += $"{i}\n" +
                        $"{item.Offset.ToString(@"hh\:mm\:ss\,fff")} --> {(item.Offset.Add(item.Duration)).ToString(@"hh\:mm\:ss\,fff")}\n" +
                        $"{item.Text}\n\n";
                    i++;
                }
                result.Add(new LyricItem { Language = track.Language.Name, Content = text});
            }
            return result;
        }
        public class YoutubeSearchResult
        {
            public string Id { get; set; }
            public string Url { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Duration { get; set; }
            public string Thumbnail { get; set; }
        }

    }
}
