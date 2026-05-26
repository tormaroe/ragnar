using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Ragnar.Natives;

public static class ZipFunctions
{
    public static void Add(Context ctx)
    {
        // native-zip [archive] [sources] [force] [verbose] [level]
        ctx.Set("native-zip", new Native((args, refs, context, interpreter, _) =>
        {
            string archivePath = args[0] is File f ? f.Path : args[0].ToUserString();
            Value sourcesVal = args[1];
            bool force = args[2] is Logic l && l.Condition;
            bool verbose = args[3] is Logic lv && lv.Condition;
            string levelStr = args[4].ToUserString().ToLower();

            // 1. Resolve compression level
            CompressionLevel compLevel = CompressionLevel.Optimal;
            if (levelStr == "fastest") compLevel = CompressionLevel.Fastest;
            else if (levelStr == "none") compLevel = CompressionLevel.NoCompression;

            // 2. Handle archive existence
            if (System.IO.File.Exists(archivePath))
            {
                if (force)
                {
                    if (verbose) context.Output.WriteLine($"Overwriting existing archive: {archivePath}");
                    System.IO.File.Delete(archivePath);
                }
                else
                {
                    throw new Exception($"Destination archive already exists: {archivePath}");
                }
            }

            // Ensure destination directory exists
            string? destDir = System.IO.Path.GetDirectoryName(archivePath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // 3. Resolve list of source paths
            var sources = new List<string>();
            if (sourcesVal is Block b)
            {
                sources.AddRange(b.Children.Skip(b.Index).Select(val => val is File fs ? fs.Path : val.ToUserString()));
            }
            else
            {
                sources.Add(sourcesVal is File fs ? fs.Path : sourcesVal.ToUserString());
            }

            // 4. Create the zip archive
            using (var archiveStream = new FileStream(archivePath, FileMode.Create))
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create))
            {
                foreach (var source in sources)
                {
                    if (Directory.Exists(source))
                    {
                        AddDirectoryToArchive(archive, source, source, verbose, compLevel, context.Output);
                    }
                    else if (System.IO.File.Exists(source))
                    {
                        AddFileToArchive(archive, source, System.IO.Path.GetFileName(source), verbose, compLevel, context.Output);
                    }
                    else
                    {
                        throw new Exception($"Source not found: {source}");
                    }
                }
            }

            return args[0];
        }, 5).WithTitle("Internal native helper to create a zip archive."));

        // native-unzip [archive] [dest] [force] [verbose]
        ctx.Set("native-unzip", new Native((args, refs, context, interpreter, _) =>
        {
            string archivePath = args[0] is File f ? f.Path : args[0].ToUserString();
            string destPath = args[1] is File fd ? fd.Path : args[1].ToUserString();
            bool force = args[2] is Logic l && l.Condition;
            bool verbose = args[3] is Logic lv && lv.Condition;

            if (!System.IO.File.Exists(archivePath))
            {
                throw new Exception($"Archive file not found: {archivePath}");
            }

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(destPath, entry.FullName));

                    // Zip slip validation
                    string destFullPath = System.IO.Path.GetFullPath(destPath);
                    if (!fullPath.StartsWith(destFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"Extracting entry {entry.FullName} would escape destination directory.");
                    }

                    if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    {
                        Directory.CreateDirectory(fullPath);
                        continue;
                    }

                    string? parentDir = System.IO.Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    if (System.IO.File.Exists(fullPath))
                    {
                        if (force)
                        {
                            if (verbose) context.Output.WriteLine($"Overwriting: {entry.FullName}");
                            System.IO.File.Delete(fullPath);
                        }
                        else
                        {
                            throw new Exception($"Destination file already exists: {fullPath}");
                        }
                    }

                    if (verbose) context.Output.WriteLine($"Extracting: {entry.FullName}");
                    entry.ExtractToFile(fullPath, overwrite: force);
                }
            }

            return args[1];
        }, 4).WithTitle("Internal native helper to extract a zip archive."));
    }

    private static void AddFileToArchive(ZipArchive archive, string filePath, string entryName, bool verbose, CompressionLevel level, TextWriter output)
    {
        if (verbose) output.WriteLine($"Adding file: {entryName}");
        archive.CreateEntryFromFile(filePath, entryName, level);
    }

    private static void AddDirectoryToArchive(ZipArchive archive, string dirPath, string rootDirPath, bool verbose, CompressionLevel level, TextWriter output)
    {
        var files = Directory.GetFiles(dirPath);
        var subDirs = Directory.GetDirectories(dirPath);

        string rootFullPath = System.IO.Path.GetFullPath(rootDirPath);

        foreach (var file in files)
        {
            string fileFullPath = System.IO.Path.GetFullPath(file);
            string relativePath = System.IO.Path.GetRelativePath(rootFullPath, fileFullPath).Replace("\\", "/");
            AddFileToArchive(archive, file, relativePath, verbose, level, output);
        }

        foreach (var subDir in subDirs)
        {
            AddDirectoryToArchive(archive, subDir, rootDirPath, verbose, level, output);
        }
    }
}
