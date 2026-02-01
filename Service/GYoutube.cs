using Floaty_Music.Models;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
namespace Floaty_Music.Service
{
    public static class GYoutube
    {
        private static readonly FloatlyContext db = new FloatlyContext();
        public static async Task<List<YoutubeSearchResult>> SearchAsync(
    string query = "official music video",
    int count = 5)
        {
#if DEBUG
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"Start fetching : {query}");
#endif

            var results = new List<YoutubeSearchResult>();

            var youtube = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = GlobalConfiguration.YT_API_KEY,
                ApplicationName = "Floatly"
            });

            var searchRequest = youtube.Search.List("snippet");
            searchRequest.Q = query;
            searchRequest.Type = "video";
            searchRequest.MaxResults = count * 2;

            var searchResponse = await searchRequest.ExecuteAsync();

            var videoIds = searchResponse.Items
                .Select(i => i.Id.VideoId)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            // Filter hidden videos in ONE query (same logic as before)
            var hiddenIds = await db.YoutubeSongs
                .AsNoTracking()
                .Where(x => x.Hidden && videoIds.Contains(x.UrlId))
                .Select(x => x.UrlId)
                .ToListAsync();

            var hiddenSet = hiddenIds.ToHashSet();

            // Fetch video details (duration, thumbnails, etc.)
            var videoRequest = youtube.Videos.List("contentDetails,snippet");
            videoRequest.Id = string.Join(",", videoIds);

            var videoResponse = await videoRequest.ExecuteAsync();

            foreach (var video in videoResponse.Items)
            {
                if (results.Count >= count)
                    break;

                if (hiddenSet.Contains(video.Id))
                    continue;

                var duration = System.Xml.XmlConvert
                    .ToTimeSpan(video.ContentDetails.Duration);

                // minimum 30 seconds
                if (duration < TimeSpan.FromSeconds(30))
                    continue;

                results.Add(new YoutubeSearchResult
                {
                    Id = video.Id,
                    Url = $"https://youtu.be/{video.Id}",
                    Title = video.Snippet.Title,
                    Author = video.Snippet.ChannelTitle,
                    Duration = duration.Hours > 0
                        ? duration.ToString(@"hh\:mm\:ss")
                        : duration.ToString(@"mm\:ss"),
                    Thumbnail = video.Snippet.Thumbnails.Maxres?.Url
                                 ?? video.Snippet.Thumbnails.High?.Url
                });
            }

#if DEBUG
            sw.Stop();
            Console.WriteLine($"Elapsed time: {sw.Elapsed}");
#endif

            return results;
        }
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
