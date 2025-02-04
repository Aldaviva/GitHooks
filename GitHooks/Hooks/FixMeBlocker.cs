using GitHooks.Tasks;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace GitHooks.Hooks;

public partial class FixMeBlocker: PreCommitHook {

    /// 100 MB
    private const long MAX_FILE_SIZE = 100 * 1024 * 1024;

    [GeneratedRegex(@"\bFIXME\b", RegexOptions.IgnoreCase)]
    private static partial Regex disallowedTokenPattern();

    private static readonly ISet<string> TEXT_FILE_EXTENSIONS = (FrozenSet<string>) [
        ".ahk", ".am", ".bash", ".bat", ".c", ".cc", ".cmd", ".config", ".cpp", ".cs", ".csproj", ".css", ".csx", ".cxx", ".dtd", ".editorconfig", ".erl", ".fs", ".fsi", ".fsscript", ".fsx",
        ".gitattributes", ".gitignore", ".gitmodules", ".groovy", ".gsh", ".gvy", ".gy", ".gyp", ".h", ".h++", ".hh", ".hm", ".hpp", ".htm", ".html", ".hxx", ".ini", ".java", ".js", ".json", ".jsx",
        ".kt", ".kts", ".less", ".manifest", ".md", ".nsh", ".nsi", ".php", ".properties", ".props", ".ps1", ".pubxml", ".py", ".rb", ".rc", ".reg", ".resx", ".rs", ".runsettings", ".sh", ".sln",
        ".src", ".svg", ".swift", ".targets", ".toml", ".ts", ".tsx", ".txt", ".vcxproj", ".xaml", ".xml", ".xsd", ".xsl", ".xslt", ".yaml", ".yml"
    ];

    public async Task<PreCommitHook.HookResult> run(IEnumerable<string> stagedFiles) {
        CancellationTokenSource cts = new();
        IEnumerable<string> stagedTextFiles = stagedFiles
            .Where(filename => TEXT_FILE_EXTENSIONS.Contains(Path.GetExtension(filename).ToLowerInvariant()));

        FilePosition? firstProblem = await Task2.firstOrDefault(stagedTextFiles.Select(async Task<FilePosition?> (filename) => {
            if (new FileInfo(filename).Length <= MAX_FILE_SIZE) {
                string fileContents = await Git.readStagedFile(filename);
                Match  match        = disallowedTokenPattern().Match(fileContents);
                if (match.Success) {
                    return stringOffsetToLine(filename, fileContents, match.Index);
                }
            }

            return null;
        }), task => task.Result != null, cts);

        if (firstProblem is { } problem) {
            Console.WriteLine(
                $"""
                 Found FIXME. Get rid of the temporary hacks and use `git add; git commit` to proceed.
                 {problem.filename}:{problem.lineNumber:N0}:{problem.columnNumber:N0} {problem.line.Trim()}
                 """);
            await cts.CancelAsync();
            return PreCommitHook.HookResult.ABORT_COMMIT;
        } else {
            return PreCommitHook.HookResult.PROCEED_WITH_COMMIT;
        }
    }

    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="offset"/> is larger than <paramref name="contents"/></exception>
    private static FilePosition stringOffsetToLine(string filename, string contents, int offset) {
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

        return new FilePosition(filename, lineNumber, columnNumber, line);
    }

    private readonly record struct FilePosition(string filename, int lineNumber, int columnNumber, string line);

}