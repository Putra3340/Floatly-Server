using Floaty_Music.Models.WebSocket;
using Floaty_Music.Service;
using Microsoft.AspNetCore.SignalR;

public class StatusHub : Hub
{
    private readonly IImportPlaylistJobQueue _queue;

    public StatusHub(IImportPlaylistJobQueue queue)
    {
        _queue = queue;
    }

    public async Task StartImportPlaylistJob(ImportPlaylistRequest request)
    {
        var jobId = Guid.NewGuid().ToString();

        await _queue.EnqueueAsync(
            jobId,
            request,
            Context.ConnectionId
        );

        await Clients.Caller.SendAsync(
            "StatusUpdate",
            $"Job queued ✨ ({jobId})"
        );
    }
}
