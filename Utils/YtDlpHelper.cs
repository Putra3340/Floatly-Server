using Floaty_Music.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class YtDlpHelper
{
    public static string ExPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "Service", "Exec", "yt-dlp.exe");

    private static async Task<string> RunAsync(string args)
    {
        if (!File.Exists(ExPath))
            throw new FileNotFoundException("yt-dlp.exe not found", ExPath);

        var psi = new ProcessStartInfo
        {
            FileName = ExPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start yt-dlp");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception($"yt-dlp error:\n{error}");

        return output.Trim();
    }

    // 🎵 Get best audio stream URL (may be m3u8, expires)
    public static Task<string> GetBestAudioUrlAsync(string youtubeUrl)
    {
        string args =
            $"--no-playlist -f bestaudio --get-url \"{youtubeUrl}\"";

        return RunAsync(args);
    }

    // 🎧 Download audio as MP3 (stable, recommended for server)
    public static async Task<string> DownloadAudioMp3Async(string youtubeUrl, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        string template = Path.Combine(outputDir, "%(id)s.%(ext)s");

        string args =
            $"--no-playlist -x --audio-format mp3 --audio-quality 0 " +
            $"-o \"{template}\" \"{youtubeUrl}\"";

        await RunAsync(args);

        var id = ExtractVideoId(youtubeUrl);
        return Path.Combine(outputDir, $"{id}.mp3");
    }

    // 🎬 Download best video+audio as MP4 (no re-encode)
    public static async Task<string> DownloadBestMp4Async(string youtubeUrl, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        string template = Path.Combine(outputDir, "%(id)s.%(ext)s");

        string args =
            $"--no-playlist -f bv*+ba/b " +
            $"--merge-output-format mp4 " +
            $"-o \"{template}\" \"{youtubeUrl}\"";

        await RunAsync(args);

        var id = ExtractVideoId(youtubeUrl);
        return Path.Combine(outputDir, $"{id}.mp4");
    }

    private static string ExtractVideoId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid YouTube URL");

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["v"] ?? throw new Exception("Cannot extract video id");
    }
    public static async Task DownloadBestAudioAsync(string url, string output)
    {
        await RunAsync($"-f bestaudio -o \"{output}\" \"{url}\" --js-runtimes node");
    }

    public static async Task DownloadVideoWithAudioAsync(
        string url,
        string output,
        int maxHeight)
    {
        string args =
            $"-f \"bv*[height<={maxHeight}]+ba/b\" " +
            $"--merge-output-format mp4 " +
            $"-o \"{output}\" \"{url}\"";

        await RunAsync(args);
    }


    public static async Task DownloadThumbnailAsync(string url, string output)
    {
        await RunAsync($"--skip-download --write-thumbnail -o \"{output}\" \"{url}\"");
    }

    public static async Task DownloadSubtitlesAsync(string url, string folder)
    {
        Directory.CreateDirectory(folder);

        string args =
            "--no-playlist --skip-download " +
            "--write-subs --write-auto-subs " +
            "--sub-format srt " +
            "--sub-langs en,id " +              // 🌷 reduce pressure
            "--concurrent-fragments 1 " +       // 🕊 very important
            "--sleep-requests 5 " +
            "--sleep-interval 5 " +
            "--max-sleep-interval 10 " +
            "--ignore-errors " +                // 💖 don’t fail hard
            "--no-warnings " +
            "--js-runtimes node " +
            $"-o \"{Path.Combine(folder, "%(id)s")}\" " +
            $"\"{url}\"";


        await RunAsync(args);
    }
    public static List<YoutubeLyrics>? NormalizeDefaultSubtitles(string folder, string baseName)
    {
        var files = Directory.GetFiles(folder, $"{baseName}*.srt");
        if (files.Length == 0) return null;

        var lyricsList = new List<YoutubeLyrics>();
        string? defaultTarget = null;

        foreach (var file in files)
        {
            var name = Path.GetFileName(file).ToLower();

            // 🌸 detect language
            string lang =
                name.Contains("en") ? "en" :
                name.Contains("id") ? "id" :
                "und";

            bool isAuto = name.Contains("auto");

            // 🌸 choose default subtitle (en / id)
            if (lang == "en" || lang == "id")
            {
                var target = Path.Combine(folder, $"{baseName}.srt");

                // prefer manual over auto
                if (!File.Exists(target) || !isAuto)
                {
                    File.Copy(file, target, overwrite: true);
                    defaultTarget = target;
                }
            }

            lyricsList.Add(new YoutubeLyrics
            {
                FileName = Path.GetFileName(file),
                LanguageCode = lang,
                IsAuto = isAuto,
                Language = lang == "en" ? "English" :
                           lang == "id" ? "Indonesian" :
                           "Unknown",
                CreatedAt = DateTime.UtcNow
            });
        }

        return lyricsList;
    }


    public sealed class YtMetadata
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Uploader { get; set; }
        public int Duration { get; set; } // seconds
        public string Thumbnail { get; set; }
    }

    public static async Task<YtMetadata> GetMetadataAsync(string youtubeUrl)
    {
        string args =
            $"--no-playlist --dump-json --skip-download \"{youtubeUrl}\"";

        var json = await RunAsync(args);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new YtMetadata
        {
            Id = root.GetProperty("id").GetString()!,
            Title = root.GetProperty("title").GetString()!,
            Uploader = root.GetProperty("uploader").GetString() ?? "Unknown",
            Duration = root.GetProperty("duration").GetInt32(),
            Thumbnail = root.GetProperty("thumbnail").GetString()
        };
    }
}
