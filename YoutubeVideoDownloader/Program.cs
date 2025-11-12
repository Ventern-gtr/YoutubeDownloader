using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

class Program
{
    private static readonly string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
    static async Task Main()
    {
        Thread.Sleep(100);
        PrintLine("             Youtube video mp4 downloader\n - https://github.com/Ventern-gtr/YoutubeDownloader -\n");
        PrintLine("Enter YouTube video URL:");
        string url = Console.ReadLine();

        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var streams = streamManifest.GetVideoStreams()
            .OrderByDescending(s => s.VideoQuality)
            .Where(s => s.VideoCodec != null);

        PrintLine("\nAvailable qualities:");

        var uniqueStreams = streams
            .GroupBy(s => GetQualityLabel(s))
            .Select(g => g.OrderByDescending(s => s.Size.Bytes).First())
            .ToList();

        int index = 1;
        foreach (var stream in uniqueStreams)
        {
            string sizeToShow = "N/A";
            if (stream.Size.GigaBytes > 0.999)
            {
                sizeToShow = MathF.Round((float)stream.Size.GigaBytes, 1) + " GB";
            }
            else if (stream.Size.MegaBytes > 0.999)
            {
                sizeToShow = MathF.Round((float)stream.Size.MegaBytes, 1) + " MB";
            }
            else if (stream.Size.KiloBytes > 0.999)
            {
                sizeToShow = MathF.Round((float)stream.Size.KiloBytes, 1) + " KB";
            }
            else if (stream.Size.Bytes > 0.999)
            {
                sizeToShow = MathF.Round(stream.Size.Bytes, 1) + " Bytes";
            }
            PrintLine($"{index}: {GetQualityLabel(stream)} ({sizeToShow})");
            index++;
        }

        PrintLine($"Select quality number (1-{uniqueStreams.Count}):");
        int choice = int.Parse(Console.ReadLine());
        var selectedStream = uniqueStreams[choice - 1];

        string filename = $"{SanitizeFileName(video.Title)}.mp4";
        string filePath = Path.Combine(downloadsPath, filename);
        await youtube.Videos.Streams.DownloadAsync(selectedStream, filePath);
        PrintLine($"Downloaded to your downloads folder");
    }

    static string GetQualityLabel(IStreamInfo stream)
    {
        if (stream is IVideoStreamInfo videoStream)
        {
            return string.IsNullOrEmpty(videoStream.VideoQuality.Label) ? "Unknown Quality" : videoStream.VideoQuality.Label;
        }
        return "Unknown Quality";
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    #region Extra Methods

    private static readonly object consoleLock = new object();
    public static void PrintLine(string message, ConsoleColor color = ConsoleColor.Gray, int typingDelay = 3, bool noline = false)
    {
        try
        {
            lock (consoleLock) // prevent multiple threads from writing simultaneously
            {
                Console.ForegroundColor = color;

                foreach (char c in message)
                {
                    Console.Write(c);
                    Thread.Sleep(typingDelay);
                }

                if (!noline)
                    Console.WriteLine();

                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            lock (consoleLock)
            {
                Console.ResetColor();
                Console.WriteLine($"Error printing message: {ex.Message}");
                Console.WriteLine(message);
            }
        }
    }

    private static void PauseExit()
    {
        try
        {
            PrintLine("", ConsoleColor.Red);
            PrintLine("Press any key to close...", ConsoleColor.Red, 10);
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            PrintLine($"Error while pausing for exit: {ex.Message}", ConsoleColor.Red);
        }
    }
    #endregion
}