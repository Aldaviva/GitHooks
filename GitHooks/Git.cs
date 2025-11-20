using System.Text;

namespace GitHooks;

public static class Git {

    private static async Task<Commitish> getHeadCommitHash() =>
        (await executeGit("rev-parse", "--verify", nameof(WellKnownCommit.HEAD))).exitCode is 0 ? WellKnownCommit.HEAD : "4b825dc642cb6eb9a060e54bf8d69288fbee4904";

    public static async Task<string[]> getStagedFiles() {
        Commitish headHash = await getHeadCommitHash();
        return (await executeGit("diff-index", "--cached", "--name-only", "--diff-filter=ACMRTUXB", headHash.ToString())).stdout.Split('\n');
    }

    public static async Task stageFile(string filename) => await executeGit("add", filename);

    public static async Task<string> readStagedFile(string filename) => await executeGit("--no-pager", "show", $":{filename}") switch {
        { exitCode: 0, stdout: var stagedContents } => stagedContents,
        { exitCode: 128 }                           => await File.ReadAllTextAsync(filename, Encoding.UTF8), // file is new and only in the working directory, not staged, and in no previous commits
        { exitCode: var exitCode }                  => throw new ApplicationException($"git exited with code {exitCode}"),
    };

    private static Task<(int exitCode, string stdout, string stderr)> executeGit(params string[] arguments) => Processes.ExecFile("git", arguments);

    public enum WellKnownCommit {

        HEAD

    }

}