using Google.Apis.Services;
using Google.Apis.YouTube.v3;
namespace Floaty_Music.Service
{
    public static class GYoutube
    {
        public static async Task GetYouTubeVideos()
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyAWvW51RHQh2MWbhkia8jlHNP2CaOt-jXU",
                ApplicationName = "Feloatly"
            });
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = "My Videos"; // Replace with your search term.
            searchListRequest.MaxResults = 50;
            // Call the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();
            List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            List<string> playlists = new List<string>();
            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(string.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId));
                        break;
                    case "youtube#channel":
                        channels.Add(string.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.ChannelId));
                        break;
                    case "youtube#playlist":
                        playlists.Add(string.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.PlaylistId));
                        break;
                }
            }
            Console.WriteLine(string.Format("Videos:\n{0}\n", string.Join("\n", videos)));
            Console.WriteLine(string.Format("Channels:\n{0}\n", string.Join("\n", channels)));
            Console.WriteLine(string.Format("Playlists:\n{0}\n", string.Join("\n", playlists)));
        }
    }
}
