using System.Text;

namespace GitHooks;

public static class Git {

    private static async Task<Commitish> getHeadCommitHash() =>
        (await executeGit("rev-parse", "--verify", nameof(WellKnownCommit.HEAD))).ExitCode is 0 ? WellKnownCommit.HEAD : "4b825dc642cb6eb9a060e54bf8d69288fbee4904";

    public static async Task<string[]> getStagedFiles() {
        Commitish headHash = await getHeadCommitHash();
        return (await executeGit("diff-index", "--cached", "--name-only", "--diff-filter=ACMRTUXB", headHash.ToString())).StdOut.Split('\n');
    }

    public static async Task stageFile(string filename) => await executeGit("add", filename);

    public static async Task<string> readStagedFile(string filename) => await executeGit("--no-pager", "show", $":{filename}") switch {
        { ExitCode: 0, StdOut: var stagedContents } => stagedContents,
        { ExitCode: 128 }                           => await File.ReadAllTextAsync(filename, Encoding.UTF8), // file is new and only in the working directory, not staged, and in no previous commits
        { ExitCode: var exitCode }                  => throw new ApplicationException($"git exited with code {exitCode}"),
    };

    private static Task<ProcessResult> executeGit(params string[] arguments) => Processes.ExecFile("git", arguments);

    public enum WellKnownCommit {

        HEAD

    }

}