using Floaty_Music.Models;
using Floaty_Music.Models.WebSocket;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
namespace Floaty_Music.Service
{
    public class StatusHub : Hub
    {
        private readonly FloatlyContext db;
        public StatusHub(FloatlyContext db)
        {
            this.db = db;
        }
        public async Task StartImportPlaylistJob(ImportPlaylistRequest request)
        {
            await Clients.Caller.SendAsync(
         "StatusUpdate",
         $"Importing playlist {request.Url}"
     );
            var client = new YoutubeExplode.YoutubeClient();
            var playlistId = YoutubeExplode.Playlists.PlaylistId.Parse(request.Url);
            var videos = await client.Playlists.GetVideosAsync(playlistId);

            var user = await db.Users.FirstOrDefaultAsync(x => x.Token == request.Token);
            if (user == null)
            {
                throw new Exception("Invalid User Token");
            }
            var playlist = new Playlists
            {
                Name = "Youtube Playlist",
                SpecialPlaylist = false,
                UserId = user.Id,
                CreatedAt = DateTime.Now,
            };
            await db.Playlists.AddAsync(playlist);
            await db.SaveChangesAsync();
            await Clients.Caller.SendAsync("StatusUpdate","Success Creating Playlist");

            var songs = new List<PlaylistSongs>();
            foreach (var video in videos)
            {
                await Clients.Caller.SendAsync("StatusUpdate",$"Downloading {video.Id}");

                var song = db.YoutubeSongs.FirstOrDefault(x => x.UrlId == video.Id);
                if (song == null)
                await YoutubeService.DownloadAndSaveAsync(video.Id.ToString());
                songs.Add(new PlaylistSongs
                {
                    PlaylistId = playlist.Id,
                    UrlId = video.Id.Value,
                    CreatedAt = DateTime.Now,
                });
            }
            await db.PlaylistSongs.AddRangeAsync(songs);
            await db.SaveChangesAsync();
            await Clients.Caller.SendAsync("StatusUpdate", $"Successfully Downloaded");
        }

    }
}
