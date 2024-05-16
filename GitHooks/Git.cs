using System.ComponentModel;
using System.Diagnostics;

namespace GitHooks;

public static class Git {

    public static async Task<Commitish> getHeadCommitHash() => (await executeGit("rev-parse", "--verify", "HEAD"))?.exitCode is null or 0 ? "HEAD" : "4b825dc642cb6eb9a060e54bf8d69288fbee4904";

    public static async Task<string[]> getStagedFiles() {
        Commitish headHash = await getHeadCommitHash();
        return (await executeGit("diff-index", "--cached", "--name-only", "--diff-filter=ACMRTUXB", headHash.ToString()!))?
            .standardOutput.Split('\n')!;
    }

    public static async Task stageFile(string filename) {
        await executeGit("add", filename);
    }

    private static async Task<ExecutionResult?> executeGit(params string[] arguments) {
        Process? process;
        try {
            process = Process.Start(new ProcessStartInfo("git", arguments) {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            });
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
            ExecutionResult result = new(process.ExitCode, (await stdout).Trim(), (await stderr).Trim());
            // Console.WriteLine($"git {string.Join(' ', arguments)} exited with code {result.exitCode}\n  stdout: {result.standardOutput}\n  stderr: {result.standardError}");
            return result;
        }
    }

    private record ExecutionResult(int exitCode, string standardOutput, string standardError);

    public enum WellKnownCommits {

        HEAD

    }

}