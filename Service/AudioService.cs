using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xabe.FFmpeg;
namespace Floaty_Music.Utils
{

    public static class AudioHelper
    {
        static AudioHelper()
        {
            FFmpeg.SetExecutablesPath(Path.GetFullPath("Exec/ffmpeg/bin"));
        }

        // Saves uploaded IFormFile to a temporary file.
        public static async Task<string> SaveTempAsync(IFormFile file)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(file.FileName));
            using var fs = new FileStream(tempPath, FileMode.Create);
            await file.CopyToAsync(fs);
            return tempPath;
        }

        // Compress MP3 to target bitrate (e.g. "128k", "96k"). just save as a new file.
        public static async Task CompressAsync(string inputPath, string bitrate = "128k")
        {
            var outputPath = Path.ChangeExtension(inputPath, $"_compressed_{bitrate}.mp3");
            Console.WriteLine($"Compressing... : {inputPath} into {outputPath}");
            await FFmpeg.Conversions.New().AddParameter($"-i \"{inputPath}\" -b:a {bitrate} \"{outputPath}\"", ParameterPosition.PreInput).Start();
        }

        // Split MP3 into chunks of given seconds.
        public static async Task<string[]> SplitAsync(string inputPath, int secondsPerChunk = 10)
        {
            var outputDir = Path.Combine(Path.GetDirectoryName(inputPath)!, Path.GetFileNameWithoutExtension(inputPath) + "_chunks");
            Directory.CreateDirectory(outputDir);

            await FFmpeg.Conversions.New()
                .AddParameter($"-i \"{inputPath}\" -f segment -segment_time {secondsPerChunk} -c copy \"{Path.Combine(outputDir, "chunk_%03d.mp3")}\"", ParameterPosition.PreInput)
                .Start();

            return Directory.GetFiles(outputDir, "chunk_*.mp3");
        }
    }

}
