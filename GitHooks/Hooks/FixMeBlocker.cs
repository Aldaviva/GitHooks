using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace GitHooks.Hooks;

public partial class FixMeBlocker: PreCommitHook {

    /// 50 MB
    private const long MAX_FILE_SIZE = 50 * 1024 * 1024;

    [GeneratedRegex(@"\bFIXME\b", RegexOptions.IgnoreCase)]
    private static partial Regex disallowedTokenPattern();

    private static readonly FrozenSet<string> TEXT_FILE_EXTENSIONS = [
        ".ahk", ".am", ".appxmanifest", ".bash", ".bat", ".c", ".cc", ".cmd", ".config", ".cpp", ".cs", ".csproj", ".css", ".csx", ".cxx", ".dtd", ".editorconfig", ".erl", ".fs", ".fsi", ".fsproj",
        ".fsscript", ".fsx", ".gitattributes", ".gitignore", ".gitmodules", ".groovy", ".gsh", ".gvy", ".gy", ".gyp", ".h", ".h++", ".hh", ".hm", ".hpp", ".htm", ".html", ".hxx", ".ini", ".java",
        ".js", ".json", ".jsp", ".jsx", ".kt", ".kts", ".less", ".manifest", ".md", ".nsh", ".nsi", ".php", ".properties", ".props", ".ps1", ".pubxml", ".py", ".rb", ".rc", ".reg", ".resx", ".rs",
        ".runsettings", ".sh", ".sln", ".slnx", ".src", ".svg", ".swift", ".targets", ".toml", ".ts", ".tsx", ".txt", ".vcproj", ".vcxproj", ".xaml", ".xml", ".xsd", ".xsl", ".xslt", ".yaml", ".yml"
    ];

    public async Task<PreCommitHook.HookResult> run(IEnumerable<string> stagedFiles) {
        IEnumerable<string> stagedTextFiles = stagedFiles.Where(filename => TEXT_FILE_EXTENSIONS.Contains(Path.GetExtension(filename).ToLowerInvariant()));

        FilePosition[] problems = (await Task.WhenAll(stagedTextFiles.Select(async Task<FilePosition?> (filename) => {
            if (new FileInfo(filename).Length <= MAX_FILE_SIZE) {
                string fileContents = await Git.readStagedFile(filename);
                Match  match        = disallowedTokenPattern().Match(fileContents);
                if (match.Success) {
                    return stringOffsetToLine(filename, fileContents, match.Index);
                }
            }

            return null;
        }))).Compact();

        if (problems.Length != 0) {
            Console.WriteLine(
                $"Found {problems.Length:N0} FIXME{(problems.Length >= 2 ? "s" : "")}. To continue committing, get rid of the following temporary hacks, then run `git add <file>…; git commit`.");
            foreach (FilePosition problem in problems.OrderBy(p => p.filename, StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.lineNumber).ThenBy(p => p.columnNumber)) {
                Console.WriteLine($"{problem.filename}:{problem.lineNumber:D}:{problem.columnNumber:D} {problem.line.Trim()}");
            }

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