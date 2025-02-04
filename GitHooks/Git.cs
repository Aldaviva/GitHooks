using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace GitHooks;

public static class Git {

    private record ExecutionResult(int exitCode, string standardOutput, string standardError, string cmdline);

    private static async Task<Commitish> getHeadCommitHash() =>
        (await executeGit("rev-parse", "--verify", WellKnownCommit.HEAD.ToString()))?.exitCode is null or 0 ? WellKnownCommit.HEAD : "4b825dc642cb6eb9a060e54bf8d69288fbee4904";

    public static async Task<string[]> getStagedFiles() {
        Commitish headHash = await getHeadCommitHash();
        return (await executeGit("diff-index", "--cached", "--name-only", "--diff-filter=ACMRTUXB", headHash.ToString()))?
            .standardOutput.Split('\n')!;
    }

    public static async Task stageFile(string filename) => await executeGit("add", filename);

    public static async Task<string> readStagedFile(string filename) => await executeGit("--no-pager", "show", $":{filename}") switch {
        { exitCode: 0, standardOutput: var stagedContents } => stagedContents,
        { exitCode: 128 } => await File.ReadAllTextAsync(filename, Encoding.UTF8), // file is new and only in the working directory, not staged, and in no previous commits
        { exitCode: var exitCode, cmdline: var cmdline } => throw new ApplicationException($"{cmdline} exited with code {exitCode}"),
        null => throw new ApplicationException("git show failed to start")
    };

    private static async Task<ExecutionResult?> executeGit(params string[] arguments) {
        Process? process;
        // Stopwatch gitDuration = new();
        ProcessStartInfo startInfo = new("git", arguments) {
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };

        try {
            // gitDuration.Start();
            process = Process.Start(startInfo);
        } catch (Win32Exception) {
            return null;
        }

        if (process == null) {
            return null;
        }

        using (process) {
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            // gitDuration.Stop();
            ExecutionResult result = new(process.ExitCode, (await stdout).Trim(), (await stderr).Trim(), string.Join(' ', arguments.Prepend(startInfo.FileName)));
            // Console.WriteLine($"{result.cmdline} exited with code {result.exitCode} in {gitDuration.ElapsedMilliseconds:N0} ms.");
            return result;
        }
    }

    public enum WellKnownCommit {

        HEAD

    }

}