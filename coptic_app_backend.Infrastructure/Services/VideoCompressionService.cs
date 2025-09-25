using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Domain.Interfaces;
using System.Diagnostics;
using System.IO;

namespace coptic_app_backend.Infrastructure.Services
{
    /// <summary>
    /// Video compression service using FFmpeg for mobile-optimized streaming
    /// </summary>
    public class VideoCompressionService : IVideoCompressionService
    {
        private readonly ILogger<VideoCompressionService> _logger;
        private readonly VideoCompressionSettings _settings;

        public VideoCompressionService(IConfiguration configuration, ILogger<VideoCompressionService> logger)
        {
            _logger = logger;
            _settings = new VideoCompressionSettings();
            configuration.GetSection("VideoCompression").Bind(_settings);
            
            // Set defaults if not configured
            if (string.IsNullOrEmpty(_settings.FFmpegPath))
            {
                _settings.FFmpegPath = "ffmpeg"; // Assume FFmpeg is in PATH
            }
        }

        public async Task<Stream> CompressVideoAsync(Stream inputStream, string fileName, VideoQuality quality = VideoQuality.Mobile)
        {
            return await CompressVideoFromStreamAsync(inputStream, fileName, quality);
        }

        public async Task<Stream> CompressVideoFromUrlAsync(string videoUrl, string fileName, VideoQuality quality = VideoQuality.Mobile)
        {
            try
            {
                _logger.LogInformation("Starting video compression from URL: {VideoUrl} with quality {Quality}", videoUrl, quality);

                // Download video from URL
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(10); // Allow time for large video downloads
                
                _logger.LogInformation("Downloading video from URL: {VideoUrl}", videoUrl);
                var videoData = await httpClient.GetByteArrayAsync(videoUrl);
                _logger.LogInformation("Downloaded {Size}MB from URL", videoData.Length / (1024 * 1024));

                // Create stream from downloaded data
                using var videoStream = new MemoryStream(videoData);
                return await CompressVideoFromStreamAsync(videoStream, fileName, quality);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compress video from URL: {VideoUrl}", videoUrl);
                throw new Exception("Video compression from URL failed", ex);
            }
        }

        private async Task<Stream> CompressVideoFromStreamAsync(Stream inputStream, string fileName, VideoQuality quality = VideoQuality.Mobile)
        {
            try
            {
                _logger.LogInformation("Starting video compression for {FileName} with quality {Quality}", fileName, quality);

                // Create temporary files
                var tempInputPath = Path.GetTempFileName();
                var tempOutputPath = Path.GetTempFileName();
                var outputFileName = Path.GetFileNameWithoutExtension(fileName) + "_compressed.mp4";
                var finalOutputPath = Path.Combine(Path.GetTempPath(), outputFileName);

                try
                {
                    // Write input stream to temporary file
                    using (var fileStream = File.Create(tempInputPath))
                    {
                        await inputStream.CopyToAsync(fileStream);
                    }

                    // Get compression settings based on quality
                    var compressionSettings = GetCompressionSettings(quality);

                    // Build FFmpeg command
                    var arguments = BuildFFmpegArguments(tempInputPath, finalOutputPath, compressionSettings);

                    _logger.LogInformation("Running FFmpeg with arguments: {Arguments}", arguments);

                    // Execute FFmpeg
                    var result = await ExecuteFFmpegAsync(arguments);

                    if (!result.Success)
                    {
                        _logger.LogError("FFmpeg compression failed: {Error}", result.Error);
                        throw new Exception($"Video compression failed: {result.Error}");
                    }

                    _logger.LogInformation("Video compression completed successfully. Output size: {Size} bytes", new FileInfo(finalOutputPath).Length);

                    // Return compressed video as stream
                    var compressedStream = new FileStream(finalOutputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                    
                    // Clean up temporary files
                    File.Delete(tempInputPath);
                    File.Delete(tempOutputPath);

                    return compressedStream;
                }
                catch (Exception ex)
                {
                    // Clean up temporary files on error
                    if (File.Exists(tempInputPath)) File.Delete(tempInputPath);
                    if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
                    if (File.Exists(finalOutputPath)) File.Delete(finalOutputPath);
                    
                    _logger.LogError(ex, "Error during video compression");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compress video: {FileName}", fileName);
                throw new Exception("Video compression failed", ex);
            }
        }

        public async Task<bool> IsVideoCompressionNeededAsync(Stream inputStream, long fileSize, VideoQuality quality = VideoQuality.Mobile)
        {
            try
            {
                // Check if compression is needed based on file size and quality settings
                var compressionSettings = GetCompressionSettings(quality);
                var maxSizeBytes = compressionSettings.MaxFileSizeMB * 1024 * 1024;

                if (fileSize <= maxSizeBytes)
                {
                    _logger.LogInformation("Video compression not needed. File size {Size}MB is within limit {Limit}MB", 
                        fileSize / (1024 * 1024), compressionSettings.MaxFileSizeMB);
                    return false;
                }

                // Additional checks could include video resolution, bitrate analysis, etc.
                _logger.LogInformation("Video compression needed. File size {Size}MB exceeds limit {Limit}MB", 
                    fileSize / (1024 * 1024), compressionSettings.MaxFileSizeMB);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if video compression is needed");
                return true; // Default to compression if we can't determine
            }
        }

        private CompressionSettings GetCompressionSettings(VideoQuality quality)
        {
            return quality switch
            {
                VideoQuality.Low => new CompressionSettings
                {
                    VideoBitrate = "500k",
                    AudioBitrate = "64k",
                    Resolution = "480p",
                    MaxFileSizeMB = 50,
                    Crf = 28
                },
                VideoQuality.Mobile => new CompressionSettings
                {
                    VideoBitrate = "1000k",
                    AudioBitrate = "128k",
                    Resolution = "720p",
                    MaxFileSizeMB = 100,
                    Crf = 23
                },
                VideoQuality.Medium => new CompressionSettings
                {
                    VideoBitrate = "2000k",
                    AudioBitrate = "192k",
                    Resolution = "1080p",
                    MaxFileSizeMB = 200,
                    Crf = 20
                },
                VideoQuality.High => new CompressionSettings
                {
                    VideoBitrate = "4000k",
                    AudioBitrate = "256k",
                    Resolution = "1080p",
                    MaxFileSizeMB = 300,
                    Crf = 18
                },
                _ => GetCompressionSettings(VideoQuality.Mobile)
            };
        }

        private string BuildFFmpegArguments(string inputPath, string outputPath, CompressionSettings settings)
        {
            var arguments = new List<string>
            {
                "-i", $"\"{inputPath}\"",
                "-c:v", "libx264",
                "-preset", "fast",
                "-crf", settings.Crf.ToString(),
                "-maxrate", settings.VideoBitrate,
                "-bufsize", (int.Parse(settings.VideoBitrate.Replace("k", "")) * 2) + "k",
                "-vf", $"scale=-2:{GetHeightFromResolution(settings.Resolution)}",
                "-c:a", "aac",
                "-b:a", settings.AudioBitrate,
                "-movflags", "+faststart", // Enable fast start for streaming
                "-f", "mp4",
                "-y", // Overwrite output file
                $"\"{outputPath}\""
            };

            return string.Join(" ", arguments);
        }

        private int GetHeightFromResolution(string resolution)
        {
            return resolution switch
            {
                "480p" => 480,
                "720p" => 720,
                "1080p" => 1080,
                _ => 720
            };
        }

        private async Task<FFmpegResult> ExecuteFFmpegAsync(string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _settings.FFmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                
                var output = new List<string>();
                var error = new List<string>();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.Add(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        error.Add(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                var result = new FFmpegResult
                {
                    Success = process.ExitCode == 0,
                    ExitCode = process.ExitCode,
                    Output = string.Join("\n", output),
                    Error = string.Join("\n", error)
                };

                if (!result.Success)
                {
                    _logger.LogError("FFmpeg process failed with exit code {ExitCode}. Error: {Error}", 
                        result.ExitCode, result.Error);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute FFmpeg");
                return new FFmpegResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }

    /// <summary>
    /// Video compression settings
    /// </summary>
    public class VideoCompressionSettings
    {
        public string FFmpegPath { get; set; } = "ffmpeg";
        public int MaxConcurrentCompressions { get; set; } = 2;
        public int CompressionTimeoutMinutes { get; set; } = 30;
    }

    /// <summary>
    /// Compression settings for different quality levels
    /// </summary>
    public class CompressionSettings
    {
        public string VideoBitrate { get; set; } = "1000k";
        public string AudioBitrate { get; set; } = "128k";
        public string Resolution { get; set; } = "720p";
        public int MaxFileSizeMB { get; set; } = 100;
        public int Crf { get; set; } = 23;
    }


    /// <summary>
    /// FFmpeg execution result
    /// </summary>
    public class FFmpegResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
