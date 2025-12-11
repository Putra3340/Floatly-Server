using Floaty_Music.Models;
using Floaty_Music.Models.ApiClient;
using System.Diagnostics;
using System.Reflection.Metadata;
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

        // Always use this, but make it fire and forget
        public static async Task<string> DownloadEverythingToDatabaseAsync(string youtubeUrl)
        {
            Songs db_song = new Songs();

            var videoId = VideoId.Parse(youtubeUrl);
            var manifest = await client.Videos.Streams.GetManifestAsync(videoId);

            // Audio
            var audio = manifest.GetAudioOnlyStreams()
                                .OrderByDescending(s => s.Bitrate)
                                .FirstOrDefault();
            if (audio == null)
                throw new Exception("No audio streams found.");

            var fullPath = Path.Combine();

            // ensure folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await client.Videos.Streams.DownloadAsync(audio, fullPath + ".m4a");
            db_song.MusicFilePath = Path.Combine();

            // Video
            var video = manifest.GetVideoStreams()
                                .OrderByDescending(s => s.VideoQuality)
                                .FirstOrDefault();
            if (video == null)
                throw new Exception("No video streams found.");

            await client.Videos.Streams.DownloadAsync(audio, fullPath + ".mp4");

            // Lyrics
            var lyrics = await client.Videos.ClosedCaptions.GetManifestAsync(videoId);
            var result = new List<LyricItem>();

            foreach (var track in lyrics.Tracks)
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
                result.Add(new LyricItem { Language = track.Language.Name, Content = text });
            }
            // Save lyrics to file
            foreach(var x in result)
            {
                var lyricPath = fullPath + $"_{x.Language}.srt";
                await File.WriteAllTextAsync(lyricPath, x.Content);
            }
            // Thumbnail
            var thumb = await client.Videos.GetAsync(videoId);
            var thumbUrl = thumb.Thumbnails.GetWithHighestResolution().Url;
            var thumbPath = fullPath + ".jpg";
            using (var httpClient = new HttpClient())
            {
                var imageBytes = await httpClient.GetByteArrayAsync(thumbUrl);
                await File.WriteAllBytesAsync(thumbPath, imageBytes);
            }

            return fullPath;
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

                var text = string.Join("\n", captions.Captions.Select(c => c.Text));
                result.Add(text);
            }

            return result;
        }

        // USE THIS
        public static async Task<List<YoutubeSearchResult>> SearchAsync(string query, int count = 5)
        {
#if DEBUG
            Stopwatch sw = new();
            Console.WriteLine($"Start fetching : {query}");
            sw.Start();
#endif
            var results = new List<YoutubeSearchResult>();
            var search = client.Search.GetVideosAsync(query).Where(x=>x.Duration <= TimeSpan.FromMinutes(30)); // limit 30 minutes

            await foreach (var video in search.Take(count))
            {
                results.Add(new YoutubeSearchResult
                {
                    Id = video.Id.Value,
                    Url = $"https://youtu.be/{video.Id.Value}",
                    Title = video.Title,
                    Author = video.Author.ChannelTitle,
                    Duration = video.Duration?.ToString(@"mm\:ss") ?? "Unknown",
                    Thumbnail = video.Thumbnails.GetWithHighestResolution().Url
                });
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
