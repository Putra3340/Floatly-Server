using Floaty_Music.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Floaty_Music.Controllers
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
            if (playlistId <= 0) return BadRequest("Invalid data");

            var playlist = await _context.Playlists
                .Include(x => x.Song) // so EF can cascade-remove from join table if configured
                .FirstOrDefaultAsync(x => x.Id == playlistId && x.UserId == user.Id);

            if (playlist == null) return NotFound("Playlist not found");

            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Playlist deleted successfully" });
        }
        [HttpPost("api/addplaylistsong")]
        public async Task<IActionResult> AddPlaylistSong([FromForm] string token, [FromForm] int playlistId, [FromForm] int songId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            if (playlistId <= 0 || songId <= 0) return BadRequest("Invalid data");

            var playlist = await _context.Playlists
                .Include(x => x.Song)
                .FirstOrDefaultAsync(x => x.Id == playlistId && x.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            var song = await _context.Songs.FindAsync(songId);
            if (song == null) return NotFound("Song not found");

            if (playlist.Song.Any(x => x.Id == songId))
                return Conflict("Song already in playlist");

            playlist.Song.Add(song);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Song added to playlist successfully" });
        }
        [HttpPost("api/removeplaylistsong")]
        public async Task<IActionResult> RemovePlaylistSong([FromForm] string token, [FromForm] int playlistId, [FromForm] int songId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            if (playlistId <= 0 || songId <= 0) return BadRequest("Invalid data");

            var playlist = await _context.Playlists
                .Include(x => x.Song)
                .FirstOrDefaultAsync(x => x.Id == playlistId && x.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            var song = playlist.Song.FirstOrDefault(x => x.Id == songId);
            if (song == null) return NotFound("Song not found in playlist");

            playlist.Song.Remove(song);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Song removed from playlist successfully" });
        }

        [HttpPost("api/editplaylist")]
        public async Task<IActionResult> EditPlaylist([FromForm] string token, [FromForm] int playlistId, [FromForm] string name)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            if (playlistId <= 0 || string.IsNullOrWhiteSpace(name)) return BadRequest("Invalid data");

            var playlist = await _context.Playlists.FirstOrDefaultAsync(x => x.Id == playlistId && x.UserId == user.Id);
            if (playlist == null) return NotFound("Playlist not found");

            playlist.Name = name.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Playlist updated successfully" });
        }
        [HttpPost("api/getplaylistsongs")]
        public async Task<IActionResult> GetPlaylistSongs([FromForm] string token, [FromForm] int playlistId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Token == token);
            if (user == null) return Unauthorized("Invalid Token");
            if (playlistId <= 0) return BadRequest("Invalid data");

            var playlist = await _context.Playlists
                .Include(p => p.Song)
                    .ThenInclude(s => s.Album)
                        .ThenInclude(a => a.Artist)
                .Include(p=> p.Song)
                    .ThenInclude(c=>c.SongCounter)
                .FirstOrDefaultAsync(p => p.Id == playlistId && p.UserId == user.Id);

            if (playlist == null) return NotFound("Playlist not found");

            var songs = playlist.Song
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.SongCounter.MusicLength,
                    s.CoverImagePath,
                    Album = s.Album != null ? s.Album.Title : "Unknown",
                    Artist = s.Album?.Artist != null ? s.Album.Artist.Name : "Unknown"
                })
                .ToList();

            return Ok(new
            {
                PlaylistId = playlist.Id,
                PlaylistName = playlist.Name,
                Songs = songs
            });
        }


    }
}
