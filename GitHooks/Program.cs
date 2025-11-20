using GitHooks;
using GitHooks.Hooks;

PackageLockService packageLockService = new();

ICollection<PreCommitHook> precommitHooks = [
    new FixMeBlocker(),
    new ILLinkRemover(packageLockService),
    new PackageLockDevDependencyBlocker(packageLockService)
];

string[] stagedFiles = await Git.getStagedFiles();

PreCommitHook.HookResult[] hookResults = await Task.WhenAll(precommitHooks.Select(hook => hook.run(stagedFiles)));

return hookResults.Any(result => result == PreCommitHook.HookResult.ABORT_COMMIT) ? 1 : 0;