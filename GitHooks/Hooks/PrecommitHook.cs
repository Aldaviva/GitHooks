namespace GitHooks.Hooks;

public interface PrecommitHook {

    /// <summary>
    /// Run a hook before getting a commit message from the user and creating a new commit.
    /// </summary>
    /// <param name="stagedFiles">file paths that have been staged in the Git index for committing, relative to the Git repository base directory</param>
    /// <returns><see cref="HookResult.PROCEED_WITH_COMMIT"/> if the commit may proceed, or <see cref="HookResult.ABORT_COMMIT"/> if the commit must not proceed</returns>
    Task<HookResult> run(IEnumerable<string> stagedFiles);

    public enum HookResult {

        PROCEED_WITH_COMMIT,
        ABORT_COMMIT

    }

}