using Floaty_Music.Models;
using Floaty_Music.Models.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;

namespace Floaty_Music.Controllers.ClientController
{
    [Route("api/playlist")]
    public class PlaylistController : ControllerBase
    {
        private readonly FloatlyContext _context;
        public PlaylistController(FloatlyContext cont)
        {
            _context = cont;
        }
        [HttpGet]
        public async Task<IActionResult> GetPlaylist([FromQuery] string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlists = await _context.Playlists
                .Include(x => x.PlaylistSongs).ThenInclude(x => x.Song)
                .Include(x => x.PlaylistSongs).ThenInclude(x => x.Url)
                .Where(x => x.UserId == user.Id)
                .ToListAsync();

            return Ok(playlists.Select(p =>
            {
                var first = p.PlaylistSongs.FirstOrDefault();

                return new
                {
                    p.Id,
                    p.Name,
                    Cover = !string.IsNullOrEmpty(first?.Song?.CoverImagePath)
                        ? $"{Request.Scheme}://{Request.Host}/uploads/cover/" + first.Song.CoverImagePath
                        : $"{Request.Scheme}://{Request.Host}/uploads/yt/" + first?.Url?.Thumbnail,
                    TotalSongs = $"{p.PlaylistSongs.Count} Songs",
                    p.CreatedAt
                };
            }));

        }
        [HttpGet("getsongs")]
        public async Task<IActionResult> GetPlaylistSongs([FromQuery] string token, [FromQuery] int playlistId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlist = await _context.Playlists
                .Include(x => x.PlaylistSongs)
                    .ThenInclude(x => x.Song)
                        .ThenInclude(x => x.Album)
                            .ThenInclude(x => x.Artist)
                .Include(x => x.PlaylistSongs)
                    .ThenInclude(x => x.Song)
                        .ThenInclude(x => x.SongCounter)
                .Include(x => x.PlaylistSongs)
                    .ThenInclude(x => x.Url)
                        .ThenInclude(x => x.SongCounter)

                 .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            List<ApiSong> combinedsong = new();
            var baseUrl = $"{Request.Scheme}://{Request.Host}/";

            foreach (var pl in playlist.PlaylistSongs)
            {
                if (pl.Song != null)
                {
                    combinedsong.Add(new ApiSong
                    {
                        Id = pl.Song.Id.ToString(),
                        Title = pl.Song.Title,
                        ArtistName = pl.Song.Album.Artist.Name,
                        Cover = baseUrl + "uploads/cover/" + pl.Song.CoverImagePath,
                        SongLength = TimeSpan.FromSeconds((double)pl.Song.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                        PlayCount = (pl.Song.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                    });
                }
                if (pl.Url != null)
                {
                    combinedsong.Add(new ApiSong
                    {
                        Id = pl.Url.UrlId.ToString(),
                        Title = pl.Url.Title,
                        ArtistName = pl.Url.AuthorName,
                        Cover = baseUrl + "uploads/yt/" + pl.Url.Thumbnail,
                        SongLength = TimeSpan.FromSeconds((double)pl.Url.SongCounter.FirstOrDefault().MusicLength).ToString(@"mm\:ss"),
                        PlayCount = (pl.Url.SongCounter.FirstOrDefault().TotalPlayed ?? 0).ToString("N0") + " Plays"
                    });
                }
            }
            return Ok(new
            {
                Songs = combinedsong
            });
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreatePlaylist([FromForm] string token, [FromForm] string name)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Invalid data");
            var playlist = new Playlists
            {
                UserId = user.Id,
                Name = name.Trim(),
                SpecialPlaylist = false,
                CreatedAt = DateTime.Now
            };
            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Playlist created successfully"});
        }
        [HttpPost("edit")]
        public async Task<IActionResult> EditPlaylist([FromForm] string token, [FromForm] int playlistId, [FromForm] string name)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            // Must not special playlist
            var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id && p.SpecialPlaylist == false);
            if (playlist == null) return NotFound("Playlist not found");

            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Invalid name");

            playlist.Name = name.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Playlist updated successfully" });
        }
        [HttpPost("delete")]
        public async Task<IActionResult> DeletePlaylist([FromForm] string token, [FromForm] int playlistId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            // Must not special playlist
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistSongs)
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id && p.SpecialPlaylist == false);

            if (playlist == null) return NotFound("Playlist not found");

            // Remove related PlaylistSongs first
            _context.PlaylistSongs.RemoveRange(playlist.PlaylistSongs);
            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Playlist deleted successfully" });
        }
        [HttpPost("addsong")]
        public async Task<IActionResult> AddPlaylistSong([FromForm] string token, [FromForm] int playlistId, [FromForm] string songId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            if (int.TryParse(songId,out int songint))
            {
                var song = await _context.Songs.FirstOrDefaultAsync(x=>x.Id == songint);
                if (song == null) return NotFound("Song not found");
                var exists = await _context.PlaylistSongs.AnyAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songint);
                if (exists) return Conflict("Song already in playlist");
                _context.PlaylistSongs.Add(new PlaylistSongs
                {
                    PlaylistId = playlistId,
                    SongId = songint,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                var song = await _context.YoutubeSongs.FirstOrDefaultAsync(x=>x.UrlId == songId);
                if (song == null) return NotFound("Song not found");
                var exists = await _context.PlaylistSongs.AnyAsync(ps => ps.PlaylistId == playlistId && ps.UrlId == songId);
                if (exists) return Conflict("Song already in playlist");
                _context.PlaylistSongs.Add(new PlaylistSongs
                {
                    PlaylistId = playlistId,
                    UrlId = songId,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Song added to playlist successfully" });
        }
        [HttpPost("removesong")]
        public async Task<IActionResult> RemovePlaylistSong([FromForm] string token, [FromForm] int playlistId, [FromForm] string songId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            if (int.TryParse(songId, out int songint))
            {
                var entry = await _context.PlaylistSongs
                .FirstOrDefaultAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songint && ps.Playlist.UserId == user.Id);

                if (entry == null) return NotFound("Song not found in playlist");

                _context.PlaylistSongs.Remove(entry);
                await _context.SaveChangesAsync();

            }
            else
            {
                var entry = await _context.PlaylistSongs
.FirstOrDefaultAsync(ps => ps.PlaylistId == playlistId && ps.UrlId == songId && ps.Playlist.UserId == user.Id);

                if (entry == null) return NotFound("Song not found in playlist");

                _context.PlaylistSongs.Remove(entry);
                await _context.SaveChangesAsync();

            }


            return Ok(new { message = "Song removed from playlist successfully" });
        }
        [HttpPost("addlikesong")]
        public async Task<IActionResult> AddLikePlaylistSong([FromForm] string token, [FromForm] string songId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.SpecialPlaylist == true && p.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            if (int.TryParse(songId,out int songint))
            {
                var song = await _context.Songs.FirstOrDefaultAsync(x=>x.Id == songint);
                if (song == null) return NotFound("Song not found");
                var exists = await _context.PlaylistSongs.AnyAsync(ps => ps.PlaylistId == playlist.Id && ps.SongId == songint);
                if (exists) return Conflict("Song already in playlist");
                _context.PlaylistSongs.Add(new PlaylistSongs
                {
                    PlaylistId = playlist.Id,
                    SongId = songint,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                var song = await _context.YoutubeSongs.FirstOrDefaultAsync(x=>x.UrlId == songId);
                if (song == null) return NotFound("Song not found");
                var exists = await _context.PlaylistSongs.AnyAsync(ps => ps.PlaylistId == playlist.Id && ps.UrlId == songId);
                if (exists) return Conflict("Song already in playlist");
                _context.PlaylistSongs.Add(new PlaylistSongs
                {
                    PlaylistId = playlist.Id,
                    UrlId = songId,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Song added to playlist successfully" });
        }
        [HttpPost("removelikesong")]
        public async Task<IActionResult> RemoveLikePlaylistSong([FromForm] string token, [FromForm] string songId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.SpecialPlaylist == true && p.UserId == user.Id);
            if (user == null) return Unauthorized("Contact the admin");
            if (int.TryParse(songId, out int songint))
            {

                var entry = await _context.PlaylistSongs
                .FirstOrDefaultAsync(ps => ps.PlaylistId == playlist.Id && ps.SongId == songint && ps.Playlist.UserId == user.Id);

                if (entry == null) return NotFound("Song not found in playlist");
                _context.PlaylistSongs.Remove(entry);
                await _context.SaveChangesAsync();
            }
            else
            {
                var entry = await _context.PlaylistSongs
                .FirstOrDefaultAsync(ps => ps.PlaylistId == playlist.Id && ps.UrlId == songId && ps.Playlist.UserId == user.Id);

                if (entry == null) return NotFound("Song not found in playlist");
                _context.PlaylistSongs.Remove(entry);
                await _context.SaveChangesAsync();
            }

                return Ok(new { message = "Song removed from playlist successfully" });
        }
    }
}
