using Floaty_Music.Models;
using Floaty_Music.Models.ApiClient;
using Microsoft.AspNetCore.Components.Forms;

namespace Floaty_Music.Service
{
    public static class YoutubeServiceAlt
    {
        private static readonly FloatlyContext db = new FloatlyContext();

        // Pending Download
        private static readonly HashSet<string> pending = new();
        private static readonly object _lock = new();

        public static async Task<ApiSongPlay> DownloadAndSaveAsync(string youtubeUrl,string httpurl)
        {
            lock (_lock)
            {
                if (!pending.Add(youtubeUrl))
                    return null;
            }
            ApiSongPlay result = null;
            try
            {
                // yeah we never receive a full url, just the video id
                var videoId = youtubeUrl;
                Directory.CreateDirectory(GlobalConfiguration.YoutubePath);

                var baseName = videoId;
                var audioFile = baseName + ".m4a";
                var videoFile = baseName + ".mp4";
                var videoTempFile = baseName + "_temp.mp4";
                var thumbFile = baseName;

                var audioPath = Path.Combine(GlobalConfiguration.YoutubePath, audioFile);
                var videoPath = Path.Combine(GlobalConfiguration.YoutubePath, videoFile);
                var videoTempPath = Path.Combine(GlobalConfiguration.YoutubePath, videoTempFile);
                var thumbPath = Path.Combine(GlobalConfiguration.YoutubePath, thumbFile);

                // 🔍 duration check (metadata only, fast)
                var meta = await YtDlpHelper.GetMetadataAsync(youtubeUrl);
                if (meta.Duration > 1800) return null; // > 30 min

                // 🎵 audio
                await YtDlpHelper.DownloadBestAudioAsync(youtubeUrl, audioPath);

                // 🎥 low-res video
                await YtDlpHelper.DownloadVideoWithAudioAsync(youtubeUrl, videoTempPath, maxHeight: 360);
                await FFmpegHelper.ReencodeAsync(videoTempPath, videoPath);


                // 🖼 thumbnail
                await YtDlpHelper.DownloadThumbnailAsync(youtubeUrl, thumbPath);

                using var trx = await db.Database.BeginTransactionAsync();

                var dbSong = new YoutubeSongs
                {
                    Title = meta.Title,
                    UrlId = baseName,
                    Music = audioFile,
                    Video = videoFile,
                    Lyrics = baseName + ".srt",
                    Thumbnail = thumbFile + ".webp",
                    AuthorName = meta.Uploader,
                    CreatedAt = DateTime.UtcNow
                };

                await db.YoutubeSongs.AddAsync(dbSong);

                await db.SongCounter.AddAsync(new SongCounter
                {
                    Url = dbSong,
                    TotalPlayed = 1,
                    MusicLength = meta.Duration
                });

                await db.SaveChangesAsync();
                await trx.CommitAsync();

                // 📝 subtitles (auto + manual)
                //await YtDlpHelper.DownloadSubtitlesAsync(youtubeUrl, GlobalConfiguration.YoutubePath);


                result = new ApiSongPlay
                {
                    Id = youtubeUrl,
                    Title = meta.Title,
                    Music = httpurl + audioFile,
                    Cover = httpurl + thumbFile + ".webp",
                    Banner = httpurl + thumbFile + ".webp",
                    Lyrics = httpurl + "empty.srt", // give default lyrics
                    MoviePath = httpurl + videoFile,
                    UploadedBy = "YouTube",
                    SongLength = (TimeSpan.FromSeconds(meta.Duration)) is TimeSpan d
                                    ? (d.Hours > 0
                                        ? d.ToString(@"hh\:mm\:ss")
                                        : d.ToString(@"mm\:ss"))
                                    : "Unknown",
                    PlayCount = "",
                    CreatedAt = DateTime.Now,
                    ArtistName = "Youtube",
                    ArtistId = null,
                    AlbumTitle = null,
                    AlbumId = 0,
                    IsLiked = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                lock (_lock)
                    pending.Remove(youtubeUrl);
            }

            return result;
        }
    }
}
