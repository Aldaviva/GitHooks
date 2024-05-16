﻿using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHooks.Hooks;

// ReSharper disable once InconsistentNaming - avoid triggering this check when committing to this repository
public partial class Fix_MeBlocker: PrecommitHook {

    /// <summary>
    /// 100 MB
    /// </summary>
    private const long MAX_FILE_SIZE = 100 * 1024 * 1024;

    [GeneratedRegex("\\bFIX" + "ME\\b", RegexOptions.IgnoreCase)]
    private static partial Regex disallowedTokenPattern();

    private static readonly ISet<string> TEXT_FILE_EXTENSIONS = new HashSet<string> {
        ".bash", ".bat", ".cmd", ".cs", ".csproj", ".css", ".groovy", ".htm", ".html", ".ini", ".java", ".js", ".json", ".less", ".md", ".nsh", ".nsi", ".php", ".properties", ".ps1", ".pubxml", ".sh",
        ".sln", ".svg", ".xaml", ".xml"
    }.ToFrozenSet();

    public async Task<PrecommitHook.HookResult> run(IEnumerable<string> stagedFiles) {
        CancellationTokenSource cts = new();
        IEnumerable<string> stagedTextFiles = stagedFiles
            .Where(filename => TEXT_FILE_EXTENSIONS.Contains(Path.GetExtension(filename).ToLowerInvariant()));

        var firstProblemFinderTask = new TaskCompletionSource<(string filename, int lineNumber, int columnNumber, string line)>();

        IEnumerable<Task> fileReadTasks = stagedTextFiles.Select(async filename => {
            if (new FileInfo(filename).Length <= MAX_FILE_SIZE) {
                string fileContents = await File.ReadAllTextAsync(filename, Encoding.UTF8, cts.Token);
                Match  match        = disallowedTokenPattern().Match(fileContents);
                if (match.Success) {
                    (int lineNumber, int columnNumber, string? line) = stringOffsetToLine(fileContents, match.Index);
                    firstProblemFinderTask.TrySetResult((filename, lineNumber, columnNumber, line));
                }
            }
        });

        await Task.WhenAny(firstProblemFinderTask.Task, Task.WhenAll(fileReadTasks));

        if (firstProblemFinderTask.Task.IsCompletedSuccessfully) {
            (string filename, int lineNumber, int columnNumber, string line) = firstProblemFinderTask.Task.Result;
            Console.WriteLine("Found FIX" + $"ME. Get rid of the temporary hacks and use `git add; git commit` to proceed.\n{filename}:{lineNumber:N0}:{columnNumber:N0} {line.Trim()}");
            await cts.CancelAsync();
            return PrecommitHook.HookResult.ABORT_COMMIT;
        } else {
            return PrecommitHook.HookResult.PROCEED_WITH_COMMIT;
        }
    }

    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="offset"/> is larger than <paramref name="contents"/></exception>
    private static (int lineNumber, int columnNumber, string line) stringOffsetToLine(string contents, int offset) {
        if (offset >= contents.Length) {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"must be less than {contents.Length:N0}, the length of {nameof(contents)}");
        }

        int lineNumber      = 0;
        int lineStartOffset = 0;
        int currentOffset   = 0;

        while (currentOffset <= offset && currentOffset != -1) {
            lineStartOffset = currentOffset;
            lineNumber++;
            currentOffset = contents.IndexOf('\n', currentOffset) + 1;
        }

        int    columnNumber = offset - lineStartOffset + 1;
        string line         = currentOffset == -1 ? contents[lineStartOffset..] : contents[lineStartOffset..currentOffset].TrimEnd('\r');

        return (lineNumber, columnNumber, line);
    }

}