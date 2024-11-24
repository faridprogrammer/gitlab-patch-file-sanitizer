using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    private static readonly ConcurrentDictionary<string, DateTime> processedFiles = new ConcurrentDictionary<string, DateTime>();

    static async Task Main(string[] args)
    {
        // Check if the folder path is provided as a command-line argument
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: dotnet run <folder_path_to_watch>");
            return;
        }

        string watchFolderPath = args[0];

        // Check if the provided path exists and is a directory
        if (!Directory.Exists(watchFolderPath))
        {
            Console.WriteLine($"Invalid folder path: {watchFolderPath}");
            return;
        }

        // Create a new watcher
        using (var watcher = new FileSystemWatcher(watchFolderPath))
        {
            watcher.Filter = "*.patch"; // Only monitor .patch files
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

            // Attach event handlers for new and changed files
            watcher.Created += OnChanged;
            watcher.Changed += OnChanged;

            watcher.EnableRaisingEvents = true; // Start watching
            Console.WriteLine($"Watching folder: {watchFolderPath}");
            Console.WriteLine("Press 'q' to quit.");

            // Keep the program running until 'q' is pressed
            while (Console.ReadKey().Key != ConsoleKey.Q) ;
        }
    }

    private static void OnChanged(object sender, FileSystemEventArgs e)
    {
        // Throttle events to avoid reprocessing the same file immediately after saving
        if (processedFiles.TryGetValue(e.FullPath, out DateTime lastProcessed))
        {
            // If the last process time is within 1 second, skip processing
            if ((DateTime.Now - lastProcessed).TotalSeconds < 1)
            {
                return;
            }
        }

        // Update the last processed time for the file
        processedFiles[e.FullPath] = DateTime.Now;

        Task.Delay(500).ConfigureAwait(false).GetAwaiter().GetResult();
        // Run cleaning asynchronously to avoid blocking
        Task.Run(() => RetryProcessFile(e.FullPath, 5, TimeSpan.FromMilliseconds(500)));
    }

    private static void RetryProcessFile(string filePath, int maxRetries, TimeSpan delay)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Try processing the file
                CleanPatchFile(filePath);
                return; // If successful, exit the loop
            }
            catch (IOException)
            {
                // Wait before retrying
                Task.Delay(delay).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        Console.WriteLine($"Failed to process file after {maxRetries} attempts: {filePath}");
    }


    private static void CleanPatchFile(string filePath)
    {
        try
        {
            // Read file content
            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {


                // Remove Git paths or namespaces (e.g., format: namespace/sub-namespace/...)
                lines[i] = Regex.Replace(lines[i], @"\b[\w-]+(?:\/[\w-]+)+\b", string.Empty);

                // Remove commit hashes (40-character hexadecimal strings)
                lines[i] = Regex.Replace(lines[i], @"\b[0-9a-fA-F]{40}\b", "REMOVED_HASH");

                // Remove email addresses
                lines[i] = Regex.Replace(lines[i], @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "REMOVED_EMAIL");

                // Remove file system paths in formats like C:\Folder\file.txt or /folder/file.txt
                lines[i] = Regex.Replace(lines[i], @"([a-zA-Z]:\\|\/)[^\s]*", "REMOVED_PATH");

                // Remove committer name (e.g., "From: Name <email>")
                lines[i] = Regex.Replace(lines[i], @"^From:\s+.+", "From: REMOVED_COMMITTER");
                lines[i] = Regex.Replace(lines[i], @"^From:\s+.+", "From: REMOVED_COMMITTER");

                // Remove commit message starting after "Date:" line
                if (lines[i].StartsWith("Date:"))
                {
                    // Clear all lines until a "---" or file diff section (usually indicates start of actual changes)
                    i++;
                    while (i < lines.Length && !lines[i].StartsWith("---") && !lines[i].StartsWith("diff "))
                    {
                        lines[i] = "REMOVED_COMMIT_MESSAGE";
                        i++;
                    }
                    i--; // Adjust index back for loop continuation
                }

                // GORM-specific: Sanitize sensitive SQL statements (e.g., CREATE, DROP, INSERT)
                lines[i] = Regex.Replace(lines[i], @"\b(CREATE|DROP|INSERT|DELETE|ALTER)\s+TABLE\b", "REMOVED_SQL_OPERATION", RegexOptions.IgnoreCase);

                // GORM-specific: Remove database column definitions
                lines[i] = Regex.Replace(lines[i], @"`\w+`\s+(VARCHAR|TEXT|INT|BIGINT|BOOLEAN|TIMESTAMP)", "REMOVED_COLUMN_DEFINITION", RegexOptions.IgnoreCase);

                // Golang Imports: Remove sensitive imports (e.g., private repos or paths)
                lines[i] = Regex.Replace(lines[i], @"import\s+\(.*\)", "REMOVED_IMPORT", RegexOptions.IgnoreCase);
                lines[i] = Regex.Replace(lines[i], @"import\s+\"".*\""", "REMOVED_IMPORT", RegexOptions.IgnoreCase);

                // Golang-specific: Redact configuration paths or environment variables
                lines[i] = Regex.Replace(lines[i], @"os.Getenv\(\"".*\""\)", "REMOVED_ENV", RegexOptions.IgnoreCase);

                // Golang-specific: Redact hardcoded secrets (e.g., anything resembling a token or key)
                lines[i] = Regex.Replace(lines[i], @"(\""[A-Za-z0-9_-]{20,}\"")", "REMOVED_SECRET", RegexOptions.IgnoreCase);

                // GORM-specific: Redact migration creator name (e.g., "// Created by Name" or annotations)
                lines[i] = Regex.Replace(lines[i], @"\/\/\s*Created by\s+.+", "// REMOVED_CREATOR", RegexOptions.IgnoreCase);
                lines[i] = Regex.Replace(lines[i], @"@Author\(.*\)", "@REMOVED_CREATOR", RegexOptions.IgnoreCase);
            }

            // Overwrite the file with sanitized content
            File.WriteAllLines(filePath, lines);
            Console.WriteLine($"Sanitized patch file: {filePath}");

            // Update the processed file time to avoid immediate re-trigger
            processedFiles[filePath] = DateTime.Now;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
        }
    }
}
