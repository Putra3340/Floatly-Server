using Floaty_Music.Models;
using Floaty_Music.Models.ApiClient;
using Floaty_Music.Utils;
using Microsoft.AspNetCore.Components.Forms;
using System.Text;

namespace Floaty_Music.Service
{
    public static class YoutubeServiceAlt
    {
        private static readonly FloatlyContext db = new FloatlyContext();

        // Pending Download
        private static readonly HashSet<string> pending = new();
        private static readonly object _lock = new();

        public static async Task<ApiSongPlay> DownloadAndSaveAsync(string youtubeUrl, string httpurl)
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
                var lyricsFile = baseName + ".srt";
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

                var audioTask = YtDlpHelper.DownloadBestAudioAsync(youtubeUrl, audioPath);

                var videoTask = Task.Run(async () =>
                {
                    await YtDlpHelper.DownloadVideoWithAudioAsync(
                        youtubeUrl,
                        videoTempPath,
                        maxHeight: 360);

                    await FFmpegHelper.ReencodeAsync(videoTempPath, videoPath);
                });

                var thumbTask = YtDlpHelper.DownloadThumbnailAsync(youtubeUrl, thumbPath);


                // 1 FEBRUARY 2026 : WE NEED TO TURN OFF THE SUBTITLE UNTIL YOUTUBEEXPLODE IS BACK
                //var subtitleTask = YtDlpHelper.DownloadSubtitlesAsync(youtubeUrl, GlobalConfiguration.YoutubePath);

                var lyrics = await YoutubeService.GetLyrics(youtubeUrl);
                var priority = new[] { "English", "Indonesia", "Japan", "Korea" };
                var firstlyrics = lyrics
                    .OrderBy(l =>
                    {
                        int idx = Array.IndexOf(priority, l.Language);
                        return idx == -1 ? int.MaxValue : idx; // unknown languages go last
                    })
                    .FirstOrDefault();
                if (firstlyrics != null)
                {
                    string lyricname = await FileHelper.SaveTextAsync($"{youtubeUrl}.srt", firstlyrics.Content, FileHelper.UploadFolder.YT);
                }

                await Task.WhenAll(audioTask, videoTask, thumbTask);
                var lyricsList =
                    YtDlpHelper.NormalizeDefaultSubtitles(
                        GlobalConfiguration.YoutubePath,
                        videoId
                    );

                lyricsFile = lyricsList == null
                    ? null
                    : $"{videoId}.srt";

                using var trx = await db.Database.BeginTransactionAsync();

                var dbSong = new YoutubeSongs
                {
                    Title = meta.Title,
                    UrlId = baseName,
                    Music = audioFile,
                    Video = videoFile,
                    Lyrics = lyricsFile,
                    Thumbnail = thumbFile + ".webp",
                    AuthorName = meta.Uploader,
                    YoutubeLyrics = lyricsList, // 🌷 attach children
                    CreatedAt = DateTime.Now
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
                try
                {
                    var captionsManifest = await YoutubeService.client.Videos.ClosedCaptions.GetManifestAsync(videoId);

                    foreach (var track in captionsManifest.Tracks)
                    {
                        var captions = await YoutubeService.client.Videos.ClosedCaptions.GetAsync(track);

                        var sb = new StringBuilder();
                        int i = 1;

                        foreach (var c in captions.Captions)
                        {
                            sb.AppendLine(i.ToString());
                            sb.AppendLine(
                                $"{c.Offset:hh\\:mm\\:ss\\,fff} --> {(c.Offset + c.Duration):hh\\:mm\\:ss\\,fff}"
                            );
                            sb.AppendLine(c.Text);
                            sb.AppendLine();
                            i++;
                        }

                        var lang = track.Language.Code ?? "und";
                        var isAuto = track.IsAutoGenerated || lang == "und";

                        var fileName = isAuto
                            ? $"{baseName}_auto.srt"
                            : $"{baseName}_{lang}.srt";

                        var fullPath = Path.Combine(GlobalConfiguration.YoutubePath, fileName);
                        await System.IO.File.WriteAllTextAsync(fullPath, sb.ToString());

                        db.YoutubeLyrics.Add(new YoutubeLyrics
                        {
                            Song = dbSong,
                            Language = track.Language.Name,
                            LanguageCode = lang,
                            IsAuto = isAuto,
                            FileName = fileName
                        });
                    }
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }



                result = new ApiSongPlay
                {
                    Id = youtubeUrl,
                    Title = meta.Title,
                    Music = httpurl + audioFile,
                    Cover = httpurl + thumbFile + ".webp",
                    Banner = httpurl + thumbFile + ".webp",
                    Lyrics = httpurl + (lyricsFile == null ? "empty.srt" : lyricsFile), // give default lyrics
                    MoviePath = httpurl + videoFile,
                    UploadedBy = meta.Uploader,
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
