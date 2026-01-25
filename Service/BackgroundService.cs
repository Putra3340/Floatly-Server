using Floaty_Music.Models;
using Floaty_Music.Models.WebSocket;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace Floaty_Music.Service
{
    public interface IImportPlaylistJobQueue
    {
        Task EnqueueAsync(string jobId, ImportPlaylistRequest request, string connectionId);
    }

    public class ImportPlaylistWorker : BackgroundService, IImportPlaylistJobQueue
    {
        private readonly Channel<JobItem> _queue = Channel.CreateUnbounded<JobItem>();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<StatusHub> _hub;

        public ImportPlaylistWorker(
            IServiceScopeFactory scopeFactory,
            IHubContext<StatusHub> hub)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
        }

        public async Task EnqueueAsync(string jobId, ImportPlaylistRequest request, string connectionId)
        {
            await _queue.Writer.WriteAsync(new JobItem(jobId, request, connectionId));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                Console.WriteLine($"Starting job {job.JobId}");
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FloatlyContext>();

                await RunJobAsync(db, job);
            }
        }

        private async Task RunJobAsync(FloatlyContext db, JobItem job)
        {
            var req = job.Request;

            await SafeSend(job.ConnectionId, $"Importing playlist {req.Url}");

            var user = await db.Users.FirstOrDefaultAsync(x => x.Token == req.Token);
            if (user == null) return;

            var playlist = new Playlists
            {
                Name = "Youtube Playlist",
                SpecialPlaylist = false,
                UserId = user.Id,
                CreatedAt = DateTime.Now
            };

            await db.Playlists.AddAsync(playlist);
            await db.SaveChangesAsync();

            await SafeSend(job.ConnectionId, "Playlist created ✨");

            var yt = new YoutubeExplode.YoutubeClient();
            var playlistId = YoutubeExplode.Playlists.PlaylistId.Parse(req.Url);
            var videos = await yt.Playlists.GetVideosAsync(playlistId);

            var songs = new List<PlaylistSongs>();

            foreach (var video in videos)
            {
                await SafeSend(job.ConnectionId, $"Downloading {video.Id}");

                var exists = await db.YoutubeSongs
                    .AnyAsync(x => x.UrlId == video.Id.ToString());

                if (!exists)
                    await YoutubeService.DownloadAndSaveAsync(video.Id.ToString());

                
                songs.Add(new PlaylistSongs
                {
                    PlaylistId = playlist.Id,
                    UrlId = video.Id.Value,
                    CreatedAt = DateTime.Now
                });
            }
            foreach(var song in songs)
            {
                var existss = await db.YoutubeSongs
                    .AnyAsync(x => x.UrlId == song.UrlId);
                if (!existss)
                    continue;
                await db.PlaylistSongs.AddAsync(song);
            }
            await db.SaveChangesAsync();

            await SafeSend(job.ConnectionId, "Import completed 🌸");
        }

        private async Task SafeSend(string connectionId, string message)
        {
            try
            {
                Debug.WriteLine($"Sending to {connectionId}: {message}");
                await _hub.Clients.Client(connectionId)
                    .SendAsync("StatusUpdate", message);
            }
            catch
            {
                // client disconnected — job continues silently
            }
        }

        private record JobItem(string JobId, ImportPlaylistRequest Request, string ConnectionId);
    }
}
