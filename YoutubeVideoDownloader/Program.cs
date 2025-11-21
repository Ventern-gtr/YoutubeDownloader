using AngleSharp.Dom;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

class Program
{
    private static readonly string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
    private static string audioFilePathSave;
    static async Task Main()
    {
        Thread.Sleep(100);
        PrintLine("             Youtube video mp4 downloader\n - https://github.com/Ventern-gtr/YoutubeDownloader -\n");
        PrintLine("Enter YouTube video URL:");
        string url = Console.ReadLine();

        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var videoStreams = streamManifest.GetVideoStreams()
            .OrderByDescending(s => s.VideoQuality)
            .Where(s => s.VideoCodec != null)
            .ToList();

        PrintLine("\nAvailable qualities:");

        int index = 1;
        var seenStreams = new HashSet<string>();
        var filteredStreams = new List<IStreamInfo>();

        foreach (var stream in videoStreams)
        {
            string streamKey = $"{GetQualityLabel(stream)}";

            if (seenStreams.Contains(streamKey))
            {
                continue;
            }

            seenStreams.Add(streamKey);
            filteredStreams.Add(stream);

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

        PrintLine($"Select quality number (1-{filteredStreams.Count}):");
        int choice;
        if (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > filteredStreams.Count)
        {
            PrintLine("Invalid choice");
            return;
        }

        var selectedStream = filteredStreams[choice - 1];
        string videoFileName = $"{SanitizeFileName(video.Title)}.mp4";

        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string videoFilePath = Path.Combine(downloadsPath, videoFileName);
        await youtube.Videos.Streams.DownloadAsync(selectedStream, videoFilePath);
        PrintLine($"Video downloaded to {videoFilePath}");
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