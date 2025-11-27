using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;

namespace Floaty_Music.Service
{
    public static class YoutubeService
    {
        private static readonly YoutubeClient client = new YoutubeClient();

        // ============================
        // DOWNLOAD AUDIO (M4A)
        // ============================
        public static async Task<string> DownloadAudioAsync(string youtubeUrl, string outputPath)
        {
            var videoId = VideoId.Parse(youtubeUrl);
            var manifest = await client.Videos.Streams.GetManifestAsync(videoId);

            var audio = manifest.GetAudioOnlyStreams()
                                .OrderByDescending(s => s.Bitrate)
                                .FirstOrDefault();

            if (audio == null)
                throw new Exception("No audio streams found.");

            await client.Videos.Streams.DownloadAsync(audio, outputPath);

            return outputPath;
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
        // DOWNLOAD THUMBNAIL
        // ============================
        public static async Task<string> DownloadThumbnailAsync(string youtubeUrl, string outputPath)
        {
            var videoId = VideoId.Parse(youtubeUrl);
            var video = await client.Videos.GetAsync(videoId);

            var thumbUrl = video.Thumbnails
                .OrderByDescending(x => x.Resolution.Area)
                .First().Url;

            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(thumbUrl);
            await File.WriteAllBytesAsync(outputPath, bytes);

            return outputPath;
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

        // ============================
        // COMBINED
        // ============================
        public static async Task<(string audioPath, string thumbPath, List<string> lyrics)>
            DownloadAllAsync(string youtubeUrl, string outputBase)
        {
            var id = VideoId.Parse(youtubeUrl);

            var audioPath = Path.Combine(outputBase, $"{id}.m4a");
            var thumbPath = Path.Combine(outputBase, $"{id}.jpg");

            await DownloadAudioAsync(youtubeUrl, audioPath);
            await DownloadThumbnailAsync(youtubeUrl, thumbPath);
            var lyrics = await GetLyricsAsync(youtubeUrl);

            return (audioPath, thumbPath, lyrics);
        }
        public static async Task<List<YoutubeSearchResult>> SearchAsync(string query, int count = 5)
        {
            var results = new List<YoutubeSearchResult>();
            var search = client.Search.GetVideosAsync(query);

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

            return results;
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
