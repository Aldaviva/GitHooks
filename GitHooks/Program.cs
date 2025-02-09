using GitHooks;
using GitHooks.Hooks;

ICollection<PreCommitHook> precommitHooks = [
    new FixMeBlocker(),
    new ILLinkRemover()
];

string[] stagedFiles = await Git.getStagedFiles();

PreCommitHook.HookResult[] hookResults = await Task.WhenAll(precommitHooks.Select(hook => hook.run(stagedFiles)));

return hookResults.Any(result => result == PreCommitHook.HookResult.ABORT_COMMIT) ? 1 : 0;