using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ragnar;

public static class ReplHistoryManager
{
    private const string HistoryFileName = ".ragnar_history";
    public const int MaxHistoryEntries = 1000;

    private static Channel<string> _writeChannel;
    private static Task _writerTask;
    private static readonly string _defaultHistoryFilePath;
    private static string? _overrideHistoryFilePath;

    static ReplHistoryManager()
    {
        _defaultHistoryFilePath = GetDefaultHistoryFilePath();
        // Initialize channel and task
        _writeChannel = CreateChannel();
        _writerTask = StartBackgroundWriter();
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Shutdown();
    }

    private static Channel<string> CreateChannel()
    {
        return Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public static string HistoryFilePath
    {
        get => _overrideHistoryFilePath ?? _defaultHistoryFilePath;
        set => _overrideHistoryFilePath = value;
    }

    private static string GetDefaultHistoryFilePath()
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(homeDir))
            {
                return System.IO.Path.Combine(homeDir, HistoryFileName);
            }
        }
        catch { }
        return System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), HistoryFileName);
    }

    private static Task StartBackgroundWriter()
    {
        return Task.Run(async () =>
        {
            var reader = _writeChannel.Reader;
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var line))
                {
                    await AppendToFileWithRetryAsync(line);
                }
            }
        });
    }

    private static async Task AppendToFileWithRetryAsync(string line)
    {
        int retries = 3;
        while (retries > 0)
        {
            try
            {
                await System.IO.File.AppendAllLinesAsync(HistoryFilePath, new[] { line });
                break;
            }
            catch (IOException)
            {
                retries--;
                if (retries == 0) break;
                await Task.Delay(50);
            }
            catch
            {
                break; // Stop immediately for other exceptions (like permissions)
            }
        }
    }

    public static List<string> LoadHistory()
    {
        var lines = new List<string>();
        var filePath = HistoryFilePath;
        if (!System.IO.File.Exists(filePath)) return lines;

        try
        {
            var rawLines = System.IO.File.ReadAllLines(filePath);
            foreach (var line in rawLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string trimmed = line.Trim();
                // Filter consecutive duplicates
                if (lines.Count > 0 && lines[^1] == trimmed) continue;
                lines.Add(trimmed);
            }

            if (lines.Count > MaxHistoryEntries)
            {
                lines = lines.Skip(lines.Count - MaxHistoryEntries).ToList();
                PruneHistoryFileSync(filePath, lines);
            }
        }
        catch
        {
            // Fail silently
        }

        return lines;
    }

    private static void PruneHistoryFileSync(string filePath, List<string> lines)
    {
        try
        {
            string tempFile = filePath + ".tmp";
            System.IO.File.WriteAllLines(tempFile, lines);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Replace(tempFile, filePath, null);
            }
            else
            {
                System.IO.File.Move(tempFile, filePath);
            }
        }
        catch
        {
            try
            {
                System.IO.File.WriteAllLines(filePath, lines);
            }
            catch { }
        }
    }

    public static void AppendHistory(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        _writeChannel.Writer.TryWrite(line);
    }

    public static void Shutdown()
    {
        try
        {
            _writeChannel.Writer.Complete();
        }
        catch { }
        try
        {
            _writerTask.GetAwaiter().GetResult();
        }
        catch { }
    }

    public static void ResetForTesting()
    {
        Shutdown();
        _writeChannel = CreateChannel();
        _writerTask = StartBackgroundWriter();
    }
}
