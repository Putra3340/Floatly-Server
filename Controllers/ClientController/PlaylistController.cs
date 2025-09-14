using Floaty_Music.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Floaty_Music.Controllers.ClientController
{

    public class PlaylistController : ControllerBase
    {
        private readonly FloatlyContext _context;
        public PlaylistController(FloatlyContext cont)
        {
            _context = cont;
        }
        [HttpPost("api/playlist")]
        public async Task<IActionResult> GetPlaylist([FromForm] string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlists = await _context.Playlists
                .Where(x => x.UserId == user.Id)
                .Select(x => new { x.Id, x.Name, x.CreatedAt })
                .ToListAsync();

            return Ok(playlists);
        }

        [HttpPost("api/createplaylist")]
        public async Task<IActionResult> CreatePlaylist([FromForm] string token, [FromForm] string name)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Invalid data");

            var playlist = new Playlists
            {
                Name = name.Trim(),
                UserId = user.Id,
                CreatedAt = DateTime.Now
            };

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Playlist created successfully", playlist.Id });
        }

        [HttpPost("api/deleteplaylist")]
        public async Task<IActionResult> DeletePlaylist([FromForm] string token, [FromForm] int playlistId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlist = await _context.Playlists
                .Include(p => p.PlaylistSongs)
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id);

            if (playlist == null) return NotFound("Playlist not found");

            // Remove related PlaylistSongs first
            _context.PlaylistSongs.RemoveRange(playlist.PlaylistSongs);
            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Playlist deleted successfully" });
        }

        [HttpPost("api/addplaylistsong")]
        public async Task<IActionResult> AddPlaylistSong([FromForm] string token, [FromForm] int playlistId, [FromForm] int songId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            var song = await _context.Songs.FindAsync(songId);
            if (song == null) return NotFound("Song not found");

            var exists = await _context.PlaylistSongs.AnyAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songId);
            if (exists) return Conflict("Song already in playlist");

            _context.PlaylistSongs.Add(new PlaylistSongs
            {
                PlaylistId = playlistId,
                SongId = songId,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Song added to playlist successfully" });
        }

        [HttpPost("api/removeplaylistsong")]
        public async Task<IActionResult> RemovePlaylistSong([FromForm] string token, [FromForm] int playlistId, [FromForm] int songId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var entry = await _context.PlaylistSongs
                .FirstOrDefaultAsync(ps => ps.PlaylistId == playlistId && ps.SongId == songId && ps.Playlist.UserId == user.Id);

            if (entry == null) return NotFound("Song not found in playlist");

            _context.PlaylistSongs.Remove(entry);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Song removed from playlist successfully" });
        }

        [HttpPost("api/editplaylist")]
        public async Task<IActionResult> EditPlaylist([FromForm] string token, [FromForm] int playlistId, [FromForm] string name)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Invalid name");

            playlist.Name = name.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Playlist updated successfully" });
        }

        [HttpPost("api/getplaylistsongs")]
        public async Task<IActionResult> GetPlaylistSongs([FromForm] string token, [FromForm] int playlistId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");

            var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            var songs = await _context.PlaylistSongs
                .Where(ps => ps.PlaylistId == playlistId)
                .Include(ps => ps.Song)
                    .ThenInclude(s => s.Album)
                        .ThenInclude(a => a.Artist)
                .Include(ps => ps.Song.SongCounter)
                .Select(ps => new
                {
                    ps.Song.Id,
                    ps.Song.Title,
                    ps.Song.SongCounter.MusicLength,
                    ps.Song.CoverImagePath,
                    Album = ps.Song.Album != null ? ps.Song.Album.Title : "Unknown",
                    Artist = ps.Song.Album.Artist != null ? ps.Song.Album.Artist.Name : "Unknown"
                })
                .ToListAsync();

            return Ok(new
            {
                PlaylistId = playlist.Id,
                PlaylistName = playlist.Name,
                Songs = songs
            });
        }
    }
}
