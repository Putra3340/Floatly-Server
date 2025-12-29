using Floaty_Music;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public static class FFmpegHelper
{
    public static string FFmpegPath { get; } =
    Path.Combine(AppContext.BaseDirectory,
        "Service", "Exec", "ffmpeg", "bin", "ffmpeg.exe");

    private static async Task RunAsync(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = FFmpegPath,
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start FFmpeg.");

        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception($"FFmpeg error:\n{error}");
    }

    // 🎥 Download / remux stream → MP4
    public static Task DownloadToMp4Async(string inputUrl, string outputMp4)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputMp4)!);

        string args =
            $"-y -loglevel error -stats " +
            $"-i \"{inputUrl}\" " +
            $"-c copy -movflags +faststart " +
            $"\"{outputMp4}\"";

        return RunAsync(args);
    }

    // 🔊 Merge video + audio
    public static Task MuxAsync(string videoPath, string audioPath, string outputMp4)
    {
        string args =
            $"-y -loglevel error -stats " +
            $"-i \"{videoPath}\" -i \"{audioPath}\" " +
            $"-c copy " +
            $"\"{outputMp4}\"";

        return RunAsync(args);
    }

    // ✨ Re-encode (if MediaPlayer is picky)
    public static Task ReencodeAsync(string input, string output)
    {
        string args =
            $"-y -loglevel error -stats " +
            $"-i \"{input}\" " +
            $"-c:v libx264 -pix_fmt yuv420p -profile:v main " +
            $"-c:a aac -movflags +faststart " +
            $"\"{output}\"";

        return RunAsync(args);
    }
}
