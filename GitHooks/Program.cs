using GitHooks;
using GitHooks.Hooks;

ICollection<PrecommitHook> precommitHooks = [
    new Fix_MeBlocker(),
    new ILLinkRemover()
];

string[] stagedFiles = await Git.getStagedFiles();

PrecommitHook.HookResult[] hookResults = await Task.WhenAll(precommitHooks.Select(hook => hook.run(stagedFiles)));

return hookResults.Any(result => result == PrecommitHook.HookResult.ABORT_COMMIT) ? 1 : 0;